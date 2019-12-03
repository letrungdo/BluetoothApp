using System;
using System.Windows.Input;
using Prism.Commands;
using Prism.Navigation;
using Prism.Services;

namespace BluetoothApp.ViewModels
{
    public class HomeViewModel : BaseViewModel
    {
        public ICommand ConnectCommand { get; }
        public HomeViewModel(INavigationService navigationService, IPageDialogService pageDialogService) : base(navigationService, pageDialogService)
        {
            Title = "Home";
            ConnectCommand = new DelegateCommand(CommandHandle);
        }

        private void CommandHandle()
        {

        }
    }
}
