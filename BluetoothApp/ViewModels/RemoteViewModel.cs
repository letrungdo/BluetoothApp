using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Input;
using BluetoothApp.Helpers;
using BluetoothApp.Models;
using BluetoothApp.Services;
using BluetoothApp.Views;
using Prism.Commands;
using Prism.Navigation;
using Prism.Services;
using Xamarin.Forms;

namespace BluetoothApp.ViewModels
{
    public class RemoteViewModel : BaseViewModel
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

        public RemoteViewModel(INavigationService navigationService, IPageDialogService pageDialogService,
            IDependencyService dependencyService)
            : base(navigationService, pageDialogService)
        {
            Title = "Remote";
            _bluetooth = dependencyService.Get<IBluetooth>();
            ItemTappedCommand = new DelegateCommand<UserBLE>(ItemTappedHandle);
            RefreshCommand = new DelegateCommand(async () => await RefreshHandle());
            RegisterReadListener();
        }

        private async Task RefreshHandle()
        {
            //await Scan
            IsRefreshing = false;
        }

        private void ItemTappedHandle(UserBLE user)
        {
            var param = new NavigationParameters();
            param.Add("data", user);
            NavigateAsync(nameof(DetailUserPage), param);
        }

        private CancellationTokenSource _tokenSource;

        public override void OnAppearing()
        {
            base.OnAppearing();
            _tokenSource?.Cancel();
            _tokenSource = new CancellationTokenSource();

            var user = new ObservableCollection<UserBLE>
            {
                new UserBLE
                {
                    Id = "dsfd", Name = "dsfdsf"
                },
                new UserBLE
                {
                    Id = "dsfd", Name = "dsfdsf"
                }
            };
            Users = user;
        }

        public override void OnDisappearing()
        {
            base.OnDisappearing();
            //_tokenSource?.Cancel();
        }

        private async void OnBleTimerElapsed(object sender, ElapsedEventArgs e)
        {
            byte[] result = null;
            try
            {
                result = await _bluetooth.ReadAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception__Read");
            }

            if (result != null)
            {
                var value = Encoding.UTF8.GetString(result);
                Debug.WriteLine(value);

                //if (value.Contains(Constant.ReScan))
                //{
                //    DataResult = "Scan lai";
                //}
                //else if (value.Contains(Constant.Added))
                //{
                //    DataResult = "Da them";
                //}
                //else if (value.Contains(Constant.Admin1))
                //{
                //    DataResult = "Tìm thấy Admin 1";
                //}
                //else if (value.Contains(Constant.Admin2))
                //{
                //    DataResult = "Tìm thấy Admin 2";
                //}

                Device.BeginInvokeOnMainThread(() =>
                {
                    DataResult = value;
                });
            }
        }

        void RegisterReadListener()
        {
            _bleTimer = new System.Timers.Timer(100);
            _bleTimer.Elapsed += OnBleTimerElapsed;
            _bleTimer.Start();
        }
    }
}
