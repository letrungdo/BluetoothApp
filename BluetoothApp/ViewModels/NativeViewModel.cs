using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Acr.UserDialogs;
using Plugin.Permissions.Abstractions;
using Prism.Commands;
using Prism.Navigation;
using Prism.Services;
using BluetoothApp.Models;
using BluetoothApp.Services;
using Xamarin.Forms;
using BluetoothApp.Views;

namespace BluetoothApp.ViewModels
{
    public class NativeViewModel : TabbedBaseViewModel
    {
        ObservableCollection<DeviceBLE> _devices = new ObservableCollection<DeviceBLE>();
        public ObservableCollection<DeviceBLE> Devices
        {
            get { return _devices; }
            set { SetProperty(ref _devices, value); }
        }

        DeviceBLE _deviceSelected;
        public DeviceBLE DeviceSelected
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
        public ICommand DisconnectCommand { get; }
        public ICommand RefreshCommand { get; }

        IPermissions _permissions;
        IUserDialogs _userDialogs;
        IBluetooth _bluetooth;

        public NativeViewModel(INavigationService navigationService, IPageDialogService pageDialogService,
        IPermissions permissions, IUserDialogs userDialogs, IDependencyService dependencyService)
        : base(navigationService, pageDialogService)
        {
            Title = "Devices Manage";
            _permissions = permissions;
            _userDialogs = userDialogs;
            _bluetooth = dependencyService.Get<IBluetooth>();
            _bluetooth.DeviceDiscovered += OnDeviceDiscovered;

            DisconnectCommand = new DelegateCommand<DeviceBLE>(DisconnectHandle);
            ItemTappedCommand = new DelegateCommand<DeviceBLE>(ItemTappedHandle);
            RefreshCommand = new DelegateCommand(async () => await RefreshHandle());
        }

        public override async void OnNavigatedTo(INavigationParameters parameters)
        {
            base.OnNavigatedTo(parameters);
            var mode = parameters.GetNavigationMode();
            switch (mode)
            {
                case NavigationMode.Back:
                case NavigationMode.Forward:
                case NavigationMode.Refresh:
                    break;
                case NavigationMode.New:
                    await _bluetooth.EnableBluetooth();

                    foreach (var connectedDevice in _bluetooth.ConnectedDevices)
                    {
                        AddOrUpdateDevice(connectedDevice);
                    }
                    break;
            }
        }

        private void OnDeviceDiscovered(object sender, DeviceEventArgs args)
        {
            AddOrUpdateDevice(args.Device);
        }

        private void AddOrUpdateDevice(DeviceBLE device)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                Devices.Add(device);
            });
        }

        private async Task RefreshHandle()
        {
            await ScanForDevices();
            IsRefreshing = false;
        }

        private async Task ScanForDevices()
        {
            if (Device.RuntimePlatform == Device.Android)
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

            Devices.Clear();
            foreach (var connectedDevice in _bluetooth.ConnectedDevices)
            {
                AddOrUpdateDevice(connectedDevice);
            }
            await _bluetooth.ScanDevicesAsync();
        }

        private void DisconnectHandle(DeviceBLE device)
        {
            if (_deviceSelected == null)
            {
                _userDialogs.Toast($"Please select a device!");
                return;
            }
            _bluetooth.Disconnect(_deviceSelected);
            var index = _devices.IndexOf(_deviceSelected);
            Devices[index].Connected = false;
        }

        private async void ItemTappedHandle(DeviceBLE device)
        {
            if (_deviceSelected == null)
            {
                _userDialogs.Toast($"Please select a device!");
                return;
            }
            _userDialogs.ShowLoading("Connecting...", MaskType.Gradient);
            await _bluetooth.ConnectAsync(_deviceSelected);

            var index = _devices.IndexOf(_deviceSelected);

            Device.BeginInvokeOnMainThread(() =>
            {
                Devices[index].Connected = !_devices[index].Connected;
                _userDialogs.HideLoading();
                var mess = _devices[index].Connected ? "Connected" : "Disonnected";
                _userDialogs.Toast($"{mess} with {_deviceSelected.Name}!");

                if (Application.Current.MainPage is TabbedPage tabbedPage)
                {
                    // navigate to remote page
                    tabbedPage.CurrentPage = tabbedPage.Children[1];
                }
            });
        }
    }
}
