using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BluetoothApp.Models;

namespace BluetoothApp.Services
{
    public interface IBluetooth
    {
        Task<bool> ConnectAsync(DeviceBLE device);
        Task<bool> Disconnect(DeviceBLE device);
        Task<bool> WriteAsync(byte[] data);
        Task<byte[]> ReadAsync();

        // 
        event EventHandler<DeviceEventArgs> DeviceDiscovered;
        IReadOnlyList<DeviceBLE> DiscoveredDevices { get; }
        IReadOnlyList<DeviceBLE> ConnectedDevices { get; }
        Task ScanDevicesAsync();
    }

    public class DeviceEventArgs : System.EventArgs
    {
        public DeviceBLE Device;
    }
}

