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
        BluetoothAdapter _btAdapter;
        static List<DeviceBLE> _newDevices = new List<DeviceBLE>();
        DeviceDiscoveredReceiver _receiver;
        static TaskCompletionSource<List<DeviceBLE>> _tcScan = new TaskCompletionSource<List<DeviceBLE>>();

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

            state = STATE_NONE;

            // Get a set of currently paired devices
            var pairedDevices = _btAdapter.BondedDevices;
        }

        public async Task<bool> ConnectAsync(DeviceBLE info)
        {
            var device = _btAdapter.GetRemoteDevice(info.Address);
            BluetoothSocket _socket;
            //Connect(device, true);
            try
            {
                _socket = device.CreateRfcommSocketToServiceRecord(MY_UUID_SECURE);
                await _socket.ConnectAsync();
            }
            catch (System.Exception ex)
            {
                // https://stackoverflow.com/questions/18657427/ioexception-read-failed-socket-might-closed-bluetooth-on-android-4-3/25647197#25647197
                _socket = (BluetoothSocket)device.Class.GetMethod("createRfcommSocket", Integer.Type).Invoke(device, 1);
                await _socket.ConnectAsync();
            }
            return true;
        }

        public Task<bool> Disconnect(DeviceBLE device)
        {
            throw new NotImplementedException();
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

        //================
        const string TAG = "BluetoothChatService";

        const string NAME_SECURE = "BluetoothChatSecure";
        const string NAME_INSECURE = "BluetoothChatInsecure";

        static UUID MY_UUID_SECURE = UUID.FromString("2b5540ab-67bf-4bfc-80d7-26380d7e00bc");
        static UUID MY_UUID_INSECURE = UUID.FromString("8ce255c0-200a-11e0-ac64-0800200c9a66");

        AcceptThread secureAcceptThread;
        AcceptThread insecureAcceptThread;
        ConnectThread connectThread;
        ConnectedThread connectedThread;
        int state;

        public const int STATE_NONE = 0;       // we're doing nothing
        public const int STATE_LISTEN = 1;     // now listening for incoming connections
        public const int STATE_CONNECTING = 2; // now initiating an outgoing connection
        public const int STATE_CONNECTED = 3;  // now connected to a remote device

        public const int MESSAGE_STATE_CHANGE = 1;
        public const int MESSAGE_READ = 2;
        public const int MESSAGE_WRITE = 3;
        public const int MESSAGE_DEVICE_NAME = 4;
        public const int MESSAGE_TOAST = 5;

        public const string DEVICE_NAME = "device_name";
        public const string TOAST = "toast";

        /// <summary>
        /// Return the current connection state.
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public int GetState()
        {
            return state;
        }

        // Start the chat service. Specifically start AcceptThread to begin a
        // session in listening (server) mode. Called by the Activity onResume()
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Start()
        {
            if (connectThread != null)
            {
                connectThread.Cancel();
                connectThread = null;
            }

            if (connectedThread != null)
            {
                connectedThread.Cancel();
                connectedThread = null;
            }

            if (secureAcceptThread == null)
            {
                secureAcceptThread = new AcceptThread(this, true);
                secureAcceptThread.Start();
            }
            if (insecureAcceptThread == null)
            {
                insecureAcceptThread = new AcceptThread(this, false);
                insecureAcceptThread.Start();
            }
        }

        /// <summary>
        /// Start the ConnectThread to initiate a connection to a remote device.
        /// </summary>
        /// <param name='device'>
        /// The BluetoothDevice to connect.
        /// </param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Connect(BluetoothDevice device, bool secure)
        {
            if (state == STATE_CONNECTING)
            {
                if (connectThread != null)
                {
                    connectThread.Cancel();
                    connectThread = null;
                }
            }

            // Cancel any thread currently running a connection
            if (connectedThread != null)
            {
                connectedThread.Cancel();
                connectedThread = null;
            }

            // Start the thread to connect with the given device
            connectThread = new ConnectThread(device, this, secure);
            connectThread.Start();
        }

        /// <summary>
        /// Start the ConnectedThread to begin managing a Bluetooth connection
        /// </summary>
        /// <param name='socket'>
        /// The BluetoothSocket on which the connection was made.
        /// </param>
        /// <param name='device'>
        /// The BluetoothDevice that has been connected.
        /// </param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Connected(BluetoothSocket socket, BluetoothDevice device, string socketType)
        {
            // Cancel the thread that completed the connection
            if (connectThread != null)
            {
                connectThread.Cancel();
                connectThread = null;
            }

            // Cancel any thread currently running a connection
            if (connectedThread != null)
            {
                connectedThread.Cancel();
                connectedThread = null;
            }


            if (secureAcceptThread != null)
            {
                secureAcceptThread.Cancel();
                secureAcceptThread = null;
            }

            if (insecureAcceptThread != null)
            {
                insecureAcceptThread.Cancel();
                insecureAcceptThread = null;
            }

            // Start the thread to manage the connection and perform transmissions
            connectedThread = new ConnectedThread(socket, this, socketType);
            connectedThread.Start();
        }

        /// <summary>
        /// Stop all threads.
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Stop()
        {
            if (connectThread != null)
            {
                connectThread.Cancel();
                connectThread = null;
            }

            if (connectedThread != null)
            {
                connectedThread.Cancel();
                connectedThread = null;
            }

            if (secureAcceptThread != null)
            {
                secureAcceptThread.Cancel();
                secureAcceptThread = null;
            }

            if (insecureAcceptThread != null)
            {
                insecureAcceptThread.Cancel();
                insecureAcceptThread = null;
            }

            state = STATE_NONE;
        }

        /// <summary>
        /// Write to the ConnectedThread in an unsynchronized manner
        /// </summary>
        /// <param name='out'>
        /// The bytes to write.
        /// </param>
        public void Write(byte[] @out)
        {
            // Create temporary object
            ConnectedThread r;
            // Synchronize a copy of the ConnectedThread
            lock (this)
            {
                if (state != STATE_CONNECTED)
                {
                    return;
                }
                r = connectedThread;
            }
            // Perform the write unsynchronized
            r.Write(@out);
        }

        /// <summary>
        /// Indicate that the connection attempt failed and notify the UI Activity.
        /// </summary>
        void ConnectionFailed()
        {
            state = STATE_LISTEN;

            Start();
        }

        /// <summary>
        /// Indicate that the connection was lost and notify the UI Activity.
        /// </summary>
        public void ConnectionLost()
        {
            state = STATE_NONE;
            this.Start();
        }

        /// <summary>
        /// This thread runs while listening for incoming connections. It behaves
        /// like a server-side client. It runs until a connection is accepted
        /// (or until cancelled).
        /// </summary>
        class AcceptThread : Thread
        {
            // The local server socket
            BluetoothServerSocket serverSocket;
            string socketType;
            BluetoothService service;

            public AcceptThread(BluetoothService service, bool secure)
            {
                BluetoothServerSocket tmp = null;
                socketType = secure ? "Secure" : "Insecure";
                this.service = service;

                try
                {
                    if (secure)
                    {
                        tmp = service._btAdapter.ListenUsingRfcommWithServiceRecord(NAME_SECURE, MY_UUID_SECURE);
                    }
                    else
                    {
                        tmp = service._btAdapter.ListenUsingInsecureRfcommWithServiceRecord(NAME_INSECURE, MY_UUID_INSECURE);
                    }

                }
                catch (Java.IO.IOException e)
                {
                    Log.Error(TAG, "listen() failed", e);
                }
                serverSocket = tmp;
                service.state = STATE_LISTEN;
            }

            public override void Run()
            {
                Name = $"AcceptThread_{socketType}";
                BluetoothSocket socket = null;

                while (service.GetState() != STATE_CONNECTED)
                {
                    try
                    {
                        socket = serverSocket.Accept();
                    }
                    catch (Java.IO.IOException e)
                    {
                        Log.Error(TAG, "accept() failed", e);
                        break;
                    }

                    if (socket != null)
                    {
                        lock (this)
                        {
                            switch (service.GetState())
                            {
                                case STATE_LISTEN:
                                case STATE_CONNECTING:
                                    // Situation normal. Start the connected thread.
                                    service.Connected(socket, socket.RemoteDevice, socketType);
                                    break;
                                case STATE_NONE:
                                case STATE_CONNECTED:
                                    try
                                    {
                                        socket.Close();
                                    }
                                    catch (Java.IO.IOException e)
                                    {
                                        Log.Error(TAG, "Could not close unwanted socket", e);
                                    }
                                    break;
                            }
                        }
                    }
                }
            }

            public void Cancel()
            {
                try
                {
                    serverSocket.Close();
                }
                catch (Java.IO.IOException e)
                {
                    Log.Error(TAG, "close() of server failed", e);
                }
            }
        }

        /// <summary>
        /// This thread runs while attempting to make an outgoing connection
        /// with a device. It runs straight through; the connection either
        /// succeeds or fails.
        /// </summary>
        protected class ConnectThread : Thread
        {
            BluetoothSocket _socket;
            BluetoothDevice _device;
            BluetoothService _service;
            string _socketType;

            public ConnectThread(BluetoothDevice device, BluetoothService service, bool secure)
            {
                _device = device;
                _service = service;
                BluetoothSocket tmp = null;
                _socketType = secure ? "Secure" : "Insecure";

                //MY_UUID_SECURE = device.GetUuids()[0].Uuid;
                try
                {
                    if (secure)
                    {
                        tmp = device.CreateRfcommSocketToServiceRecord(MY_UUID_SECURE);
                    }
                    else
                    {
                        tmp = device.CreateInsecureRfcommSocketToServiceRecord(MY_UUID_INSECURE);
                    }

                }
                catch (Java.IO.IOException e)
                {
                    Log.Error(TAG, "create() failed", e);
                }
                _socket = tmp;
                service.state = STATE_CONNECTING;
            }

            public override void Run()
            {
                Name = $"ConnectThread_{_socketType}";

                // Always cancel discovery because it will slow down connection
                _service._btAdapter.CancelDiscovery();

                // Make a connection to the BluetoothSocket
                try
                {
                    // This is a blocking call and will only return on a
                    // successful connection or an exception
                    _socket.Connect();
                }
                catch (Java.IO.IOException e)
                {
                    // Close the socket
                    try
                    {
                        _socket.Close();
                        _socket = (BluetoothSocket)_device.Class.GetMethod("createRfcommSocket", Integer.Type).Invoke(_device, 1);
                        _socket.Connect();
                    }
                    catch (Java.IO.IOException e2)
                    {
                        Log.Error(TAG, $"unable to close() {_socketType} socket during connection failure.", e2);
                    }

                    // Start the service over to restart listening mode
                    //_service.ConnectionFailed();
                    return;
                }

                // Reset the ConnectThread because we're done
                lock (this)
                {
                    _service.connectThread = null;
                }

                // Start the connected thread
                _service.Connected(_socket, _device, _socketType);
            }

            public void Cancel()
            {
                try
                {
                    _socket.Close();
                }
                catch (Java.IO.IOException e)
                {
                    Log.Error(TAG, "close() of connect socket failed", e);
                }
            }
        }

        /// <summary>
        /// This thread runs during a connection with a remote device.
        /// It handles all incoming and outgoing transmissions.
        /// </summary>
        class ConnectedThread : Thread
        {
            BluetoothSocket socket;
            Stream inStream;
            Stream outStream;
            BluetoothService service;

            public ConnectedThread(BluetoothSocket socket, BluetoothService service, string socketType)
            {
                Log.Debug(TAG, $"create ConnectedThread: {socketType}");
                this.socket = socket;
                this.service = service;
                Stream tmpIn = null;
                Stream tmpOut = null;

                // Get the BluetoothSocket input and output streams
                try
                {
                    tmpIn = socket.InputStream;
                    tmpOut = socket.OutputStream;
                }
                catch (Java.IO.IOException e)
                {
                    Log.Error(TAG, "temp sockets not created", e);
                }

                inStream = tmpIn;
                outStream = tmpOut;
                service.state = STATE_CONNECTED;
            }

            public override void Run()
            {
                Log.Info(TAG, "BEGIN mConnectedThread");
                byte[] buffer = new byte[1024];
                int bytes;

                // Keep listening to the InputStream while connected
                while (service.GetState() == STATE_CONNECTED)
                {
                    try
                    {
                        // Read from the InputStream
                        bytes = inStream.Read(buffer, 0, buffer.Length);

                    }
                    catch (Java.IO.IOException e)
                    {
                        Log.Error(TAG, "disconnected", e);
                        service.ConnectionLost();
                        break;
                    }
                }
            }

            /// <summary>
            /// Write to the connected OutStream.
            /// </summary>
            /// <param name='buffer'>
            /// The bytes to write
            /// </param>
            public void Write(byte[] buffer)
            {
                try
                {
                    outStream.Write(buffer, 0, buffer.Length);
                }
                catch (Java.IO.IOException e)
                {
                    Log.Error(TAG, "Exception during write", e);
                }
            }

            public void Cancel()
            {
                try
                {
                    socket.Close();
                }
                catch (Java.IO.IOException e)
                {
                    Log.Error(TAG, "close() of connect socket failed", e);
                }
            }
        }
    }
}
