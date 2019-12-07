using System;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using BluetoothApp.Helpers;
using BluetoothApp.Services;
using Prism.Commands;
using Prism.Navigation;
using Prism.Services;
using Xamarin.Forms;

namespace BluetoothApp.ViewModels
{
    public class RemoteViewModel : BaseViewModel
    {
        public ICommand WriteCommand { get; }
        public ICommand ReadCommand { get; }
        string _dataResult = string.Empty;
        public string DataResult
        {
            get { return _dataResult; }
            set { SetProperty(ref _dataResult, value); }
        }

        IDependencyService _dependencyService;

        public RemoteViewModel(INavigationService navigationService, IPageDialogService pageDialogService,
            IDependencyService dependencyService)
            : base(navigationService, pageDialogService)
        {
            Title = "Remote";
            _dependencyService = dependencyService;
            WriteCommand = new DelegateCommand(WriteHandle);
            ReadCommand = new DelegateCommand(async () => await ReadHandle());
        }

        private async Task ReadHandle()
        {
            while(true)
            {
                var result = await _dependencyService.Get<IBluetooth>().ReadAsync();
                if (result is null)
                    return;
                //var value = result.FromByteArray<string>();
                var value = Encoding.ASCII.GetString(result).TrimEnd('\0');
                Device.BeginInvokeOnMainThread(() =>
                {
                    DataResult = value;
                });
            }
        }

        private void WriteHandle()
        {
            //var data = "Hello".ToByteArray();
            var data = Encoding.ASCII.GetBytes("Hello ble");
            _dependencyService.Get<IBluetooth>().WriteAsync(data);
        }
    }
}
