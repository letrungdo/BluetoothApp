using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.OS;
using Android.Util;
using BluetoothApp.Droid.Services;
using BluetoothApp.Models;
using BluetoothApp.Services;
using Java.Lang;
using Java.Util;

[assembly: Xamarin.Forms.Dependency(typeof(BluetoothService))]
namespace BluetoothApp.Droid.Services
{
    public class BluetoothService : IBluetooth
    {
        // todo change MY_UUID_SECURE to UUID of target device
        static UUID MY_UUID_SECURE = UUID.FromString("fa87c0d0-afac-11de-8a39-0800200c9a66");
        const string NAME_SECURE = "BluetoothChatSecure";

        BluetoothAdapter _btAdapter;
        static List<DeviceBLE> _newDevices = new List<DeviceBLE>();
        DeviceDiscoveredReceiver _receiver;
        static TaskCompletionSource<List<DeviceBLE>> _tcScan = new TaskCompletionSource<List<DeviceBLE>>();
        BluetoothSocket _clientSocket;
        BluetoothServerSocket _serverSocket;
        Stream _inStream;

        public BluetoothService()
        {
            var ctx = Application.Context;
            // Register for broadcasts when a device is discovered
            _receiver = new DeviceDiscoveredReceiver();
            var filter = new IntentFilter(BluetoothDevice.ActionFound);
            ctx.RegisterReceiver(_receiver, filter);

            // Register for broadcasts when discovery has finished
            filter = new IntentFilter(BluetoothAdapter.ActionDiscoveryFinished);
            ctx.RegisterReceiver(_receiver, filter);

            // Get the local Bluetooth adapter
            _btAdapter = BluetoothAdapter.DefaultAdapter;

            // Get a set of currently paired devices
            var pairedDevices = _btAdapter.BondedDevices;
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
                try
                {
                    // https://stackoverflow.com/questions/18657427/ioexception-read-failed-socket-might-closed-bluetooth-on-android-4-3/25647197#25647197
                    _clientSocket = (BluetoothSocket)device.Class.GetMethod("createRfcommSocket", Integer.Type).Invoke(device, 1);
                    await _clientSocket.ConnectAsync();
                }
                catch (System.Exception ex2)
                {

                }
            }

            //try
            //{
            //    // Connect as a Server
            //    _serverSocket = _btAdapter.ListenUsingRfcommWithServiceRecord(NAME_SECURE, MY_UUID_SECURE);

            //    BluetoothSocket socket = null;

            //    // Listener
            //    while (true)
            //    {
            //        try
            //        {
            //            socket = _serverSocket.Accept();
            //        }
            //        catch (Java.IO.IOException e)
            //        {
            //            break;
            //        }

            //        if (socket != null)
            //        {
            //            lock (this)
            //            {

            //                try
            //                {
            //                    socket.Close();
            //                }
            //                catch (Java.IO.IOException e)
            //                {
            //                }
            //                break;
            //            }
            //        }
            //    }
            //}
            //catch (System.Exception ex)
            //{

            //}
            _inStream = _clientSocket.InputStream;

            return true;
        }

        public Task<bool> Disconnect(DeviceBLE device)
        {
            _clientSocket?.Close();
            _serverSocket?.Close();
            return Task.FromResult(true);
        }

        public Task<List<DeviceBLE>> Scan()
        {
            _tcScan = new TaskCompletionSource<List<DeviceBLE>>();
            _newDevices?.Clear();
            // If we're already discovering, stop it
            if (_btAdapter.IsDiscovering)
            {
                _btAdapter.CancelDiscovery();
            }
            // Request discover from BluetoothAdapter
            var x = _btAdapter.StartDiscovery();

            return _tcScan.Task;
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

        public class DeviceDiscoveredReceiver : BroadcastReceiver
        {
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
                        DeviceBLE item = new DeviceBLE
                        {
                            Name = device.Name,
                            Address = device.Address
                        };
                        _newDevices.Add(item);
                    }
                }
                else if (action == BluetoothAdapter.ActionDiscoveryFinished)
                {
                    _tcScan.TrySetResult(_newDevices);
                }
            }
        }
    }
}
