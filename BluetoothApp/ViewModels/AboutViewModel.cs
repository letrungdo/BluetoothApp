using System;
using System.Windows.Input;
using Prism.Commands;
using Prism.Navigation;
using Prism.Services;
using Xamarin.Essentials;

namespace BluetoothApp.ViewModels
{
    public class AboutViewModel : BaseViewModel
    {
        public ICommand OpenWebCommand { get; }

        public AboutViewModel(INavigationService navigationService, IPageDialogService pageDialogService) : base(navigationService, pageDialogService)
        {
            Title = "About";

            OpenWebCommand = new DelegateCommand(() => Launcher.OpenAsync(new Uri("https://github.com/letrungdo")));
        }
    }
}