using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Acr.UserDialogs;
//using BluetoothApp.Models;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.Permissions.Abstractions;
using Prism.Commands;
using Prism.Navigation;
using Prism.Services;
using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Extensions;
using Plugin.BLE.Abstractions.EventArgs;
using System.Threading;

namespace BluetoothApp.ViewModels
{
    public class HomeViewModel : BaseViewModel
    {
        ObservableCollection<IDevice> _devices = new ObservableCollection<IDevice>();
        public ObservableCollection<IDevice> Devices
        {
            get { return _devices; }
            set { SetProperty(ref _devices, value); }
        }

        IDevice _deviceSelected;
        public IDevice DeviceSelected
        {
            get { return _deviceSelected; }
            set { SetProperty(ref _deviceSelected, value); }
        }

        bool _isRefreshing;
        public bool IsRefreshing
        {
            get { return _isRefreshing; }
            set { SetProperty(ref _isRefreshing, value); }
        }

        public ICommand ItemTappedCommand { get; }
        public ICommand ConnectCommand { get; }
        public ICommand DetailCommand { get; }
        public ICommand RefreshCommand { get; }

        IBluetoothLE _bluetooth;
        IAdapter _adapter;
        IPermissions _permissions;
        IUserDialogs _userDialogs;

        public HomeViewModel(INavigationService navigationService, IPageDialogService pageDialogService,
        IBluetoothLE bluetooth, IAdapter adapter, IPermissions permissions, IUserDialogs userDialogs)
        : base(navigationService, pageDialogService)
        {
            Title = "Home";
            _bluetooth = bluetooth;
            _adapter = adapter;
            _permissions = permissions;
            _userDialogs = userDialogs;

            adapter.DeviceDiscovered += OnDeviceDiscovered;

            ConnectCommand = new DelegateCommand(ConnectHandle);
            DetailCommand = new DelegateCommand(DetailHandle);
            ItemTappedCommand = new DelegateCommand<IDevice>(ItemTappedHandle);
            RefreshCommand = new DelegateCommand(async () => await RefreshHandle());
        }

        private void OnDeviceDiscovered(object sender, DeviceEventArgs args)
        {
            AddOrUpdateDevice(args.Device);
        }

        private async Task RefreshHandle()
        {
            if (Xamarin.Forms.Device.RuntimePlatform == Xamarin.Forms.Device.Android)
            {
                var status = await _permissions.CheckPermissionStatusAsync(Permission.Location);
                if (status != PermissionStatus.Granted)
                {
                    var permissionResult = await _permissions.RequestPermissionsAsync(Permission.Location);

                    if (permissionResult.First().Value != PermissionStatus.Granted)
                    {
                        await _userDialogs.AlertAsync("Permission denied. Not scanning.");
                        _permissions.OpenAppSettings();
                        return;
                    }
                }
            }
            await ScanForDevices();
            IsRefreshing = false;
        }

        private async Task ScanForDevices()
        {
            Devices.Clear();

            foreach (var connectedDevice in _adapter.ConnectedDevices)
            {
                // update rssi for already connected evices (so tha 0 is not shown in the list)
                try
                {
                    await connectedDevice.UpdateRssiAsync();
                }
                catch (Exception ex)
                {
                    Trace.Message(ex.Message);
                    await _userDialogs.AlertAsync($"Failed to update RSSI for {connectedDevice.Name}");
                }

                AddOrUpdateDevice(connectedDevice);
            }

            _adapter.ScanMode = ScanMode.LowLatency;
            await _adapter.StartScanningForDevicesAsync();
        }

        private void AddOrUpdateDevice(IDevice device)
        {
            Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
            {
                Devices.Add(device);
            });
        }

        private void HandleSelectedDevice(IDevice device)
        {
            var config = new ActionSheetConfig();

            if (device.State == DeviceState.Connected)
            {
                config.Add("Update RSSI", async () =>
                {
                    try
                    {
                        _userDialogs.ShowLoading();

                        await device.UpdateRssiAsync();

                        _userDialogs.HideLoading();

                        _userDialogs.Toast($"RSSI updated {device.Rssi}", TimeSpan.FromSeconds(1));
                    }
                    catch (Exception ex)
                    {
                        _userDialogs.HideLoading();
                        await _userDialogs.AlertAsync($"Failed to update rssi. Exception: {ex.Message}");
                    }
                });

                config.Destructive = new ActionSheetOption("Disconnect", () => DisconnectDevice(device));
            }
            else
            {
                config.Add("Connect", async () =>
                {
                    if (await ConnectDeviceAsync(device))
                    {
                        // todo
                    }
                });
            }
            config.Cancel = new ActionSheetOption("Cancel");
            config.SetTitle("Device Options");
            _userDialogs.ActionSheet(config);
        }

        private async Task<bool> ConnectDeviceAsync(IDevice device, bool showPrompt = true)
        {
            if (showPrompt && !await _userDialogs.ConfirmAsync($"Connect to device '{device.Name}'?"))
            {
                return false;
            }

            CancellationTokenSource tokenSource = new CancellationTokenSource();
            try
            {
                var config = new ProgressDialogConfig()
                {
                    Title = $"Connecting to '{device.Id}'",
                    CancelText = "Cancel",
                    IsDeterministic = false,
                    OnCancel = tokenSource.Cancel
                };

                using (var progress = _userDialogs.Progress(config))
                {
                    progress.Show();

                    await _adapter.ConnectToDeviceAsync(device, new ConnectParameters(autoConnect: true, forceBleTransport: true), tokenSource.Token);
                }

                await _userDialogs.AlertAsync($"Connected to {device.Name}.");

                return true;

            }
            catch (Exception ex)
            {
                await _userDialogs.AlertAsync(ex.Message, "Connection error");
                Trace.Message(ex.Message);
                return false;
            }
            finally
            {
                _userDialogs.HideLoading();
                tokenSource.Dispose();
                tokenSource = null;
            }
        }

        private async Task DisconnectDevice(IDevice device)
        {
            try
            {
                if (device.State != DeviceState.Connected)
                    return;

                _userDialogs.ShowLoading($"Disconnecting {device.Name}...");

                await _adapter.DisconnectDeviceAsync(device);
            }
            catch (Exception ex)
            {
                await _userDialogs.AlertAsync(ex.Message, "Disconnect error");
            }
            finally
            {
                _userDialogs.HideLoading();
            }
        }


        private void DetailHandle()
        {

        }

        private void ItemTappedHandle(IDevice device)
        {
            HandleSelectedDevice(device);
        }

        private void ConnectHandle()
        {
            //HandleSelectedDevice(DeviceSelected);
        }
    }
}
