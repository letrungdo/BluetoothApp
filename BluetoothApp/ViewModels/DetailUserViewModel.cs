using System;
using System.Text;
using System.Windows.Input;
using BluetoothApp.Models;
using BluetoothApp.Services;
using Prism.Commands;
using Prism.Navigation;
using Prism.Services;

namespace BluetoothApp.ViewModels
{
    public class DetailUserViewModel : BaseViewModel
    {
        public ICommand AddCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand SendCommand { get; }

        string _name = "1";
        public string Name
        {
            get { return _name; }
            set { SetProperty(ref _name, value); }
        }

        IBluetooth _bluetooth;
        public DetailUserViewModel(INavigationService navigationService, IPageDialogService pageDialogService,
            IDependencyService dependencyService)
            : base(navigationService, pageDialogService)
        {
            Title = "Detail";
            _bluetooth = dependencyService.Get<IBluetooth>();
            AddCommand = new DelegateCommand(AddHandle);
            DeleteCommand = new DelegateCommand(DeleteHandle);
            SendCommand = new DelegateCommand(SendHandle);
        }

        private void SendHandle()
        {
            var data = Encoding.UTF8.GetBytes(_name + Environment.NewLine);
            _bluetooth.Write(data);
        }

        private void DeleteHandle()
        {
            var data = Encoding.ASCII.GetBytes("0" + Environment.NewLine);
            _bluetooth.Write(data);
        }

        private void AddHandle()
        {
            var data = Encoding.ASCII.GetBytes("1" + Environment.NewLine);
            _bluetooth.Write(data);
        }

        public override void OnNavigatedTo(INavigationParameters parameters)
        {
            base.OnNavigatedTo(parameters);
            var user = parameters.GetValue<UserBLE>("data");
            Title = user.Name;
        }
    }
}
