using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Input;
using Acr.UserDialogs;
using BluetoothApp.Models;
using Plugin.BluetoothLE;
using Prism.Commands;
using Prism.Navigation;
using Prism.Services;

namespace BluetoothApp.ViewModels
{
    public class ScanViewModel : BaseViewModel
    {
        IAdapter _adapter;
        IDisposable _scan;
        readonly IUserDialogs _userDialogs;

        public ICommand ScanToggle { get; }
        public ICommand OpenSettings { get; }
        public ICommand ToggleAdapterState { get; }
        public ICommand SelectDevice { get; }
        ObservableCollection<ScanResultModel> _devices = new ObservableCollection<ScanResultModel>();
        public ObservableCollection<ScanResultModel> Devices
        {
            get { return _devices; }
            set { SetProperty(ref _devices, value); }
        }
        public bool IsScanning { get; private set; }

        public ScanViewModel(INavigationService navigationService, IPageDialogService pageDialogService,
            IUserDialogs userDialogs, IAdapter adapter)
            : base(navigationService, pageDialogService)
        {
            _adapter = adapter;
            _userDialogs = userDialogs;
            SelectDevice = new DelegateCommand<ScanResultModel>(SelectdDeviceHandle);
            OpenSettings = new DelegateCommand(OpenSettingsHandle);
            ToggleAdapterState = new DelegateCommand(ToggleAdapterStateHandle);
            ScanToggle = new DelegateCommand(ScanToggleHandle);
        }

        private void ScanToggleHandle()
        {
            if (this.IsScanning)
            {
                this._scan?.Dispose();
                this.IsScanning = false;
            }
            else
            {
                this.Devices.Clear();

                this.IsScanning = true;
                this._scan = this
                    ._adapter
                    .Scan()
                    .Buffer(TimeSpan.FromSeconds(1))
                    //.ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(
                        results =>
                        {
                            var list = new ObservableCollection<ScanResultModel>();
                            foreach (var result in results)
                            {
                                var dev = this.Devices.FirstOrDefault(x => x.Uuid.Equals(result.Device.Uuid));

                                if (dev != null)
                                {
                                    dev.TrySet(result);
                                }
                                else
                                {
                                    dev = new ScanResultModel();
                                    dev.TrySet(result);
                                    list.Add(dev);
                                }
                            }
                            if (list.Any())
                                this.Devices = list;
                        },
                        ex => _userDialogs.Alert(ex.ToString(), "ERROR")
                    );
                //.DisposeWith(this.DeactivateWith);
            }
        }

        private void ToggleAdapterStateHandle()
        {
            if (_adapter.CanControlAdapterState())
            {
                var poweredOn = _adapter.Status == AdapterStatus.PoweredOn;
                _adapter.SetAdapterState(!poweredOn);
            }
            else
            {
                _userDialogs.Alert("Cannot change bluetooth adapter state");
            }
        }

        private void OpenSettingsHandle()
        {
            if (_adapter.Features.HasFlag(AdapterFeatures.OpenSettings))
            {
                _adapter.OpenSettings();
            }
            else
            {
                _userDialogs.Alert("Cannot open bluetooth settings");
            }
        }

        private void SelectdDeviceHandle(ScanResultModel obj)
        {
        }

        public override void OnNavigatingTo(INavigationParameters parameters)
        {
            base.OnNavigatedTo(parameters);
            _adapter = parameters.GetValue<IAdapter>("adapter");
            this.Title = $"{_adapter.DeviceName} ({_adapter.Status})";
        }

        public override void OnAppearing()
        {
            base.OnAppearing();
            this.IsScanning = false;
        }
    }
}
