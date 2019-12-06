using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Android.App;
using Android.Bluetooth;
using Android.Content;
using BluetoothApp.Droid.Services;
using BluetoothApp.Models;
using BluetoothApp.Services;

[assembly: Xamarin.Forms.Dependency(typeof(BluetoothService))]
namespace BluetoothApp.Droid.Services
{
    public class BluetoothService : IBluetooth
    {
        BluetoothAdapter btAdapter;
        static List<DeviceBLE> _newDevices = new List<DeviceBLE>();
        DeviceDiscoveredReceiver receiver;
        static TaskCompletionSource<List<DeviceBLE>> _tcScan = new TaskCompletionSource<List<DeviceBLE>>();

        public BluetoothService()
        {
            var ctx = Application.Context;
            // Register for broadcasts when a device is discovered
            receiver = new DeviceDiscoveredReceiver();
            var filter = new IntentFilter(BluetoothDevice.ActionFound);
            ctx.RegisterReceiver(receiver, filter);

            // Register for broadcasts when discovery has finished
            filter = new IntentFilter(BluetoothAdapter.ActionDiscoveryFinished);
            ctx.RegisterReceiver(receiver, filter);

            // Get the local Bluetooth adapter
            btAdapter = BluetoothAdapter.DefaultAdapter;

            // Get a set of currently paired devices
            var pairedDevices = btAdapter.BondedDevices;
        }

        public Task<bool> Connect()
        {
            throw new NotImplementedException();
        }

        public Task<bool> Disconnect()
        {
            throw new NotImplementedException();
        }

        public Task<List<DeviceBLE>> Scan()
        {
            _tcScan = new TaskCompletionSource<List<DeviceBLE>>();
            _newDevices?.Clear();
            // If we're already discovering, stop it
            if (btAdapter.IsDiscovering)
            {
                btAdapter.CancelDiscovery();
            }
            // Request discover from BluetoothAdapter
            var x = btAdapter.StartDiscovery();

            return _tcScan.Task;
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
                    _tcScan.SetResult(_newDevices);
                }
            }
        }
    }
}
