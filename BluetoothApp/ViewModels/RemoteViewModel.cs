using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Input;
using BluetoothApp.Models;
using BluetoothApp.Services;
using BluetoothApp.Views;
using Prism.Commands;
using Prism.Navigation;
using Prism.Services;
using Xamarin.Forms;

namespace BluetoothApp.ViewModels
{
    public class RemoteViewModel : TabbedBaseViewModel
    {
        private System.Timers.Timer _bleTimer;

        string _dataResult = string.Empty;
        public string DataResult
        {
            get { return _dataResult; }
            set { SetProperty(ref _dataResult, value); }
        }

        ObservableCollection<UserBLE> _users = new ObservableCollection<UserBLE>();
        public ObservableCollection<UserBLE> Users
        {
            get { return _users; }
            set { SetProperty(ref _users, value); }
        }

        UserBLE _userSelected;
        public UserBLE UserSelected
        {
            get { return _userSelected; }
            set { SetProperty(ref _userSelected, value); }
        }

        bool _isRefreshing;
        public bool IsRefreshing
        {
            get { return _isRefreshing; }
            set { SetProperty(ref _isRefreshing, value); }
        }

        public ICommand ItemTappedCommand { get; }
        public ICommand RefreshCommand { get; }
        IBluetooth _bluetooth;
        public ICommand AddCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand SendCommand { get; }

        string _name = "1";
        public string Name
        {
            get { return _name; }
            set { SetProperty(ref _name, value); }
        }

        public RemoteViewModel(INavigationService navigationService, IPageDialogService pageDialogService,
            IDependencyService dependencyService)
            : base(navigationService, pageDialogService)
        {
            Title = "Remote";
            _bluetooth = dependencyService.Get<IBluetooth>();
            ItemTappedCommand = new DelegateCommand<UserBLE>(ItemTappedHandle);
            RefreshCommand = new DelegateCommand(async () => await RefreshHandle());
            AddCommand = new DelegateCommand(AddHandle);
            DeleteCommand = new DelegateCommand(DeleteHandle);
            SendCommand = new DelegateCommand(SendHandle);
            RegisterReadListener();
        }

        void RegisterReadListener()
        {
            _bleTimer = new System.Timers.Timer(100);
            _bleTimer.Elapsed += OnBleTimerElapsed;
            _bleTimer.Start();
        }

        private void SendHandle()
        {
            var data = Encoding.UTF8.GetBytes(_name);
            _bluetooth.Write(data);
        }

        private void DeleteHandle()
        {
            var data = Encoding.ASCII.GetBytes("0" + Environment.NewLine);
            //var data = Encoding.ASCII.GetBytes("0");

            _bluetooth.Write(data);
        }

        private void AddHandle()
        {
            var data = Encoding.ASCII.GetBytes("1" + Environment.NewLine);
            //var data = Encoding.ASCII.GetBytes("1");

            _bluetooth.Write(data);
        }

        private async Task RefreshHandle()
        {
            //await Scan
            IsRefreshing = false;
        }

        private void ItemTappedHandle(UserBLE user)
        {
            var data = Encoding.ASCII.GetBytes(user.Id + Environment.NewLine);
            _bluetooth.Write(data);
        }

        public override void OnAppearing()
        {
            base.OnAppearing();
            var user = new ObservableCollection<UserBLE>
            {
                new UserBLE
                {
                    Id = "1", Name = "DO"
                },
                new UserBLE
                {
                    Id = "2", Name = "ABC"
                }
            };
            Users = user;
        }
        bool _reading;
        private async void OnBleTimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (_reading)
                return;
            byte[] result = null;
            try
            {
                _reading = true;
                result = await _bluetooth.ReadAsync();
            }
            catch (Exception ex)
            {
                _reading = false;
                //Debug.WriteLine(ex);
            }

            if (result != null)
            {
                var value = Encoding.UTF8.GetString(result).TrimEnd(new char[] { (char)0 });
                Debug.Write(value);
                //var display = _dataResult += value;
                //if (display.Length > 150)
                //    display.Remove(0, display.Length - 50);
                Device.BeginInvokeOnMainThread(() =>
                {
                    DataResult = value;
                    _reading = false;
                });
            }
            else
            {
                _reading = false;
            }
        }

        protected override void RaiseIsActiveChanged()
        {
            base.RaiseIsActiveChanged();
            if (IsActive)
            {
                // tab actived
            }
        }
    }
}
