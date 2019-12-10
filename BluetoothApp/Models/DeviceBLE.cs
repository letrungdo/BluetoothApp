using Prism.Mvvm;

namespace BluetoothApp.Models
{
    public class DeviceBLE : BindableBase
    {
        public string Name { get; set; }
        public string Address { get; set; }

        bool _connected;
        public bool Connected
        {
            get { return _connected; }
            set { SetProperty(ref _connected, value); }
        }
    }
}
