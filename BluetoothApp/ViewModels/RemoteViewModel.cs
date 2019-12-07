using System;
using System.Windows.Input;
using BluetoothApp.Helpers;
using BluetoothApp.Services;
using Prism.Commands;
using Prism.Navigation;
using Prism.Services;

namespace BluetoothApp.ViewModels
{
    public class RemoteViewModel : BaseViewModel
    {
        public ICommand WriteCommand { get; }
        IDependencyService _dependencyService;

        public RemoteViewModel(INavigationService navigationService, IPageDialogService pageDialogService,
            IDependencyService dependencyService)
            : base(navigationService, pageDialogService)
        {
            Title = "Remote";
            _dependencyService = dependencyService;
            WriteCommand = new DelegateCommand(WriteHandle);
        }

        private void WriteHandle()
        {
            var data = CommonHelpers.ObjectToByteArray("Hello");
            _dependencyService.Get<IBluetooth>().WriteAsync(data);
        }
    }
}
