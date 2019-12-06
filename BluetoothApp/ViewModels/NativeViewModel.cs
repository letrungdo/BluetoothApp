using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Acr.UserDialogs;
using Plugin.Permissions.Abstractions;
using Prism.Commands;
using Prism.Navigation;
using Prism.Services;
using System.Threading;
using BluetoothApp.Models;
using BluetoothApp.Services;

namespace BluetoothApp.ViewModels
{
    public class NativeViewModel : BaseViewModel
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
        public ICommand ConnectCommand { get; }
        public ICommand DetailCommand { get; }
        public ICommand RefreshCommand { get; }

        IPermissions _permissions;
        IUserDialogs _userDialogs;
        IDependencyService _dependencyService;

        public NativeViewModel(INavigationService navigationService, IPageDialogService pageDialogService,
        IPermissions permissions, IUserDialogs userDialogs, IDependencyService dependencyService)
        : base(navigationService, pageDialogService)
        {
            Title = "Native";
            _permissions = permissions;
            _userDialogs = userDialogs;
            _dependencyService = dependencyService;

            ConnectCommand = new DelegateCommand(ConnectHandle);
            DetailCommand = new DelegateCommand(DetailHandle);
            ItemTappedCommand = new DelegateCommand<DeviceBLE>(ItemTappedHandle);
            RefreshCommand = new DelegateCommand(async () => await RefreshHandle());
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
            var devices = await _dependencyService.Get<IBluetooth>().Scan();
            Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
            {
                devices?.ForEach(device =>
                {
                    Devices.Add(device);
                });
            });
        }

        private void HandleSelectedDevice(DeviceBLE device)
        {

        }

        private void DetailHandle()
        {

        }

        private void ItemTappedHandle(DeviceBLE device)
        {
            HandleSelectedDevice(device);
        }

        private void ConnectHandle()
        {
            //HandleSelectedDevice(DeviceSelected);
        }
    }
}
