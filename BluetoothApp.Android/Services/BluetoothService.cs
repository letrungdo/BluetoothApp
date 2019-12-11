using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.Bluetooth;
using Android.Content;
using BluetoothApp.Droid.Services;
using BluetoothApp.Models;
using BluetoothApp.Services;
using Java.Lang;
using Java.Util;
using Plugin.CurrentActivity;

[assembly: Xamarin.Forms.Dependency(typeof(BluetoothService))]
namespace BluetoothApp.Droid.Services
{
    public class BluetoothService : BroadcastReceiver, IBluetooth
    {
        // todo change MY_UUID_SECURE to UUID of target device
        static UUID MY_UUID_SECURE = UUID.FromString("fa87c0d0-afac-11de-8a39-0800200c9a66");
        TaskCompletionSource<bool> _tcScan = new TaskCompletionSource<bool>();
        public static TaskCompletionSource<bool> TcsEnableBluetooth = new TaskCompletionSource<bool>();
        public const int REQUEST_ENABLE_BT = 3;

        BluetoothAdapter _btAdapter;
        private readonly ConcurrentDictionary<string, DeviceBLE> _discoveredDevices = new ConcurrentDictionary<string, DeviceBLE>();
        private readonly ConcurrentDictionary<string, DeviceBLE> _connectedDevices = new ConcurrentDictionary<string, DeviceBLE>();

        BluetoothSocket _clientSocket;
        Stream _inStream;

        public IReadOnlyList<DeviceBLE> DiscoveredDevices => _discoveredDevices.Values.ToList();

        public IReadOnlyList<DeviceBLE> ConnectedDevices => _connectedDevices.Values.ToList();

        public event EventHandler<DeviceEventArgs> DeviceDiscovered;

        public BluetoothService()
        {
            var ctx = Application.Context;
            // Register for broadcasts when a device is discovered
            var filter = new IntentFilter(BluetoothDevice.ActionFound);
            ctx.RegisterReceiver(this, filter);

            // Register for broadcasts when discovery has finished
            filter = new IntentFilter(BluetoothAdapter.ActionDiscoveryFinished);
            ctx.RegisterReceiver(this, filter);

            // Get the local Bluetooth adapter
            _btAdapter = BluetoothAdapter.DefaultAdapter;

            GetBondedDevices();
        }

        void GetBondedDevices()
        {
            // Get a set of currently paired devices
            var pairedDevices = _btAdapter.BondedDevices;

            // If there are paired devices, add each on to the ArrayAdapter
            if (pairedDevices.Count > 0)
            {
                foreach (var device in pairedDevices)
                {
                    DeviceBLE item = new DeviceBLE
                    {
                        Name = device.Name,
                        Address = device.Address
                    };
                    _connectedDevices[device.Address] = item;
                }
            }
        }

        public async Task<bool> ConnectAsync(DeviceBLE info)
        {
            var device = _btAdapter.GetRemoteDevice(info.Address);
            // Connect as a Client
            try
            {
                _clientSocket = device.CreateRfcommSocketToServiceRecord(MY_UUID_SECURE);
                await _clientSocket.ConnectAsync();
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine(ex);
                try
                {
                    // https://stackoverflow.com/questions/18657427/ioexception-read-failed-socket-might-closed-bluetooth-on-android-4-3/25647197#25647197
                    _clientSocket = (BluetoothSocket)device.Class.GetMethod("createRfcommSocket", Integer.Type).Invoke(device, 1);
                    await _clientSocket.ConnectAsync();
                }
                catch (System.Exception ex2)
                {
                    Debug.WriteLine(ex2);
                }
            }
            _inStream = _clientSocket.InputStream;
            return true;
        }

        public Task<bool> Disconnect(DeviceBLE device)
        {
            _clientSocket?.Close();
            return Task.FromResult(true);
        }

        public async Task<bool> WriteAsync(byte[] data)
        {
            try
            {
                await _clientSocket.OutputStream.WriteAsync(data, 0, data.Length);
            }
            catch (Java.IO.IOException e)
            {
                return false;
            }
            return true;
        }

        public async Task<byte[]> ReadAsync()
        {
            byte[] _buffer = new byte[1024];

            try
            {
                int bytes = await _inStream.ReadAsync(_buffer, 0, _buffer.Length);
                if (bytes > 0)
                {
                    return _buffer;
                }
            }
            catch (Java.IO.IOException)
            {
            }
            return null;
        }

        public Task ScanDevicesAsync()
        {
            _tcScan = new TaskCompletionSource<bool>();
            _discoveredDevices.Clear();

            // If we're already discovering, stop it
            if (_btAdapter.IsDiscovering)
            {
                _btAdapter.CancelDiscovery();
            }
            // Request discover from BluetoothAdapter
            var x = _btAdapter.StartDiscovery();

            return _tcScan.Task;
        }

        public override void OnReceive(Context context, Intent intent)
        {
            string action = intent.Action;

            // When discovery finds a device
            if (action == BluetoothDevice.ActionFound)
            {
                // Get the BluetoothDevice object from the Intent
                BluetoothDevice device = (BluetoothDevice)intent.GetParcelableExtra(BluetoothDevice.ExtraDevice);
                // If it's already paired, skip it, because it's been listed already
                if (device.BondState != Bond.Bonded)
                {
                    if (_discoveredDevices.ContainsKey(device.Address))
                        return;
                    DeviceBLE item = new DeviceBLE
                    {
                        Name = device.Name,
                        Address = device.Address
                    };
                    _discoveredDevices[device.Address] = item;
                    DeviceDiscovered?.Invoke(this, new DeviceEventArgs { Device = item });
                }
            }
            else if (action == BluetoothAdapter.ActionDiscoveryFinished)
            {
                _tcScan.TrySetResult(true);
            }
        }

        public async Task<bool> EnableBluetooth()
        {
            // Turn on Bluetooth
            if (!_btAdapter.IsEnabled)
            {
                TcsEnableBluetooth = new TaskCompletionSource<bool>();
                var enableIntent = new Intent(BluetoothAdapter.ActionRequestEnable);
                CrossCurrentActivity.Current.Activity.StartActivityForResult(enableIntent, REQUEST_ENABLE_BT);
                await TcsEnableBluetooth.Task;
                GetBondedDevices();
            }
            return true;
        }
    }
}
