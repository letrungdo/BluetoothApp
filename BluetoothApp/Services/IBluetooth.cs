using System.Collections.Generic;
using System.Threading.Tasks;
using BluetoothApp.Models;

namespace BluetoothApp.Services
{
    public interface IBluetooth
    {
        Task<List<DeviceBLE>> Scan();
        Task<bool> ConnectAsync(DeviceBLE device);
        Task<bool> Disconnect(DeviceBLE device);
    }
}

