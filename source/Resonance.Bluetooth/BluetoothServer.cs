using InTheHand.Net.Bluetooth;
using InTheHand.Net.Sockets;
using Microsoft.Extensions.Logging;
using Resonance.Adapters.Bluetooth;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Resonance.Bluetooth
{
    /// <summary>
    /// Represents a Bluetooth Listening server capable of handling incoming Bluetooth connections.
    /// </summary>
    /// <seealso cref="Resonance.ResonanceObject" />
    /// <seealso cref="Resonance.IResonanceListeningServer{Resonance.Adapters.Bluetooth.BluetoothAdapter}" />
    public class BluetoothServer : ResonanceObject, IResonanceListeningServer<BluetoothAdapter>
    {
        private BluetoothListener _listener;
        private Thread _connectionThread;

        /// <summary>
        /// Gets a value indicating whether this server is currently listening for incoming connections.
        /// </summary>
        public bool IsStarted { get; private set; }

        /// <summary>
        /// Gets the Bluetooth address.
        /// </summary>
        public String Address { get; }

        /// <summary>
        /// Occurs when a new connection request is available.
        /// </summary>
        public event EventHandler<ResonanceListeningServerConnectionRequestEventArgs<BluetoothAdapter>> ConnectionRequest;

        /// <summary>
        /// Initializes a new instance of the <see cref="BluetoothServer"/> class.
        /// </summary>
        /// <param name="address">The Bluetooth address.</param>
        public BluetoothServer(String address)
        {
            Address = address;
            _listener = new BluetoothListener(Guid.Parse(address));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BluetoothServer"/> class.
        /// </summary>
        public BluetoothServer() : this(BluetoothService.SerialPort.ToString())
        {

        }

        /// <summary>
        /// Start listening for incoming connections.
        /// </summary>
        public void Start()
        {
            if (!IsStarted)
            {
                _listener.Start();
                IsStarted = true;

                _connectionThread = new Thread(ConnectionThreadMethod);
                _connectionThread.IsBackground = true;
                _connectionThread.Start();
            }
        }

        /// <summary>
        /// Start listening for incoming connections.
        /// </summary>
        /// <returns></returns>
        public Task StartAsync()
        {
            return Task.Factory.StartNew(Start);
        }

        /// <summary>
        /// Stop listening for incoming connections.
        /// </summary>
        public void Stop()
        {
            if (IsStarted)
            {
                IsStarted = false;

                try
                {
                    _connectionThread.Abort();
                }
                catch { }

                try
                {
                    _listener.Stop();
                }
                catch { }
            }
        }

        /// <summary>
        /// Stop listening for incoming connections.
        /// </summary>
        /// <returns></returns>
        public Task StopAsync()
        {
            return Task.Factory.StartNew(Stop);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Stop();
            _listener.Dispose();
        }

        /// <summary>
        /// Disposes component resources asynchronously.
        /// </summary>
        /// <returns></returns>
        public Task DisposeAsync()
        {
            return Task.Factory.StartNew(Dispose);
        }

        private void ConnectionThreadMethod()
        {
            while (IsStarted)
            {
                try
                {
                    var client = _listener.AcceptBluetoothClient();
                    if (client != null)
                    {
                        BluetoothClient c = new BluetoothClient();

                        BluetoothDeviceInfo info = null;

                        foreach (var device in c.PairedDevices)
                        {
                            if (device.DeviceName == client.RemoteMachineName)
                            {
                                info = device;
                                break;
                            }
                        }

                        OnConnectionRequest(new BluetoothAdapter(client,
                            info != null ? new BluetoothDevice(info) :
                            new BluetoothDevice(client.RemoteMachineName)));
                    }
                    else
                    {
                        Thread.Sleep(1000);
                    }
                }
                catch (ThreadAbortException)
                {
                    //Ignore
                    return;
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error occurred while trying to scan for incoming bluetooth connections.");
                    Thread.Sleep(1000);
                }
            }
        }

        /// <summary>
        /// Called when a new connection request has arrived.
        /// </summary>
        /// <param name="adapter">The adapter.</param>
        protected virtual void OnConnectionRequest(BluetoothAdapter adapter)
        {
            ResonanceListeningServerConnectionRequestEventArgs<BluetoothAdapter> args = new ResonanceListeningServerConnectionRequestEventArgs<BluetoothAdapter>(() =>
            {
                return adapter;
            }, () =>
            {
                adapter.Dispose();
            });

            ConnectionRequest?.Invoke(this, args);
        }
    }
}