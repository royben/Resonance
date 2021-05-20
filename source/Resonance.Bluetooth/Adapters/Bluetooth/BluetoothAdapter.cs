using InTheHand.Net;
using InTheHand.Net.Bluetooth;
using InTheHand.Net.Sockets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Resonance.Adapters.Bluetooth
{
    /// <summary>
    /// Represents a adapter capable of serial communication over Bluetooth.
    /// </summary>
    /// <seealso cref="Resonance.ResonanceAdapter" />
    public class BluetoothAdapter : ResonanceAdapter
    {
        private BluetoothClient _client;
        private NetworkStream _stream;
        private byte[] _size_buffer;

        #region Properties

        /// <summary>
        /// Gets the remote Bluetooth service name.
        /// </summary>
        public String Service { get; private set; }

        /// <summary>
        /// Gets the remote Bluetooth device address.
        /// </summary>
        public String Address { get; private set; }

        /// <summary>
        /// Gets the remote Bluetooth device information.
        /// </summary>
        public BluetoothDevice Device { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether to enable Bluetooth authentication when connecting.
        /// When true, will trigger an automatic pairing process when connecting (Secure Simple Pairing (SSP)).
        /// When false, will use insecure Bluetooth channel.
        /// </summary>
        public bool Authenticate { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="BluetoothAdapter"/> class.
        /// </summary>
        /// <param name="client">The Bluetooth client.</param>
        /// <param name="device">The Bluetooth device info.</param>
        internal BluetoothAdapter(BluetoothClient client, BluetoothDevice device)
        {
            _client = client;
            Device = device;
            Address = device?.Address;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BluetoothAdapter"/> class.
        /// </summary>
        /// <param name="address">The remote Bluetooth device address.</param>
        public BluetoothAdapter(String address)
        {
            Address = address;
            Service = BluetoothService.SerialPort.ToString();
            Authenticate = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BluetoothAdapter"/> class.
        /// </summary>
        /// <param name="address">The remote Bluetooth device address.</param>
        /// <param name="service">The remote Bluetooth device service name.</param>
        public BluetoothAdapter(String address, String service) : this(address)
        {
            Service = service;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BluetoothAdapter"/> class.
        /// </summary>
        /// <param name="device">The remote Bluetooth device.</param>
        public BluetoothAdapter(BluetoothDevice device) : this(device.Address)
        {
            Device = device;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BluetoothAdapter"/> class.
        /// </summary>
        /// <param name="device">The remote Bluetooth device.</param>
        /// <param name="service">The remote Bluetooth device service name.</param>
        public BluetoothAdapter(BluetoothDevice device, String service) : this(device.Address, service)
        {
            Device = device;
        }

        #endregion

        #region Connect / Disconnect / Write

        protected override Task OnConnect()
        {
            return Task.Factory.StartNew(() =>
            {
                if (_client == null)
                {
                    _client = new BluetoothClient();
                    _client.Authenticate = Authenticate;
                    _client.Connect(BluetoothAddress.Parse(Address), Guid.Parse(Service));

                    if (!_client.Connected)
                    {
                        throw new InvalidOperationException("Error connecting the adapter to the specified address.");
                    }
                }

                _stream = _client.GetStream();
                State = ResonanceComponentState.Connected;
                Task.Factory.StartNew(() =>
                {
                    WaitForData();
                }, TaskCreationOptions.LongRunning);
            });
        }

        protected override Task OnDisconnect()
        {
            return Task.Factory.StartNew(() =>
            {
                State = ResonanceComponentState.Disconnected;
                _client.Dispose();
            });
        }

        protected override void OnWrite(byte[] data)
        {
            data = PrependDataSize(data);
            _stream.Write(data, 0, data.Length);
        }

        #endregion

        #region Data Reading

        private void WaitForData()
        {
            try
            {
                if (State == ResonanceComponentState.Connected)
                {
                    _size_buffer = new byte[4];
                    _stream.BeginRead(_size_buffer, 0, _size_buffer.Length, EndReading, _stream);
                }
            }
            catch (Exception ex)
            {
                OnFailed(ex, "Error occurred while trying to read from bluetooth stream.");
            }
        }

        private void EndReading(IAsyncResult ar)
        {
            try
            {
                if (State == ResonanceComponentState.Connected)
                {
                    _stream.EndRead(ar);

                    int expectedSize = BitConverter.ToInt32(_size_buffer, 0);

                    if (expectedSize > 0)
                    {
                        byte[] data = new byte[expectedSize];
                        _stream.Read(data, 0, expectedSize);

                        if (State != ResonanceComponentState.Connected)
                        {
                            return;
                        }

                        OnDataAvailable(data);
                    }
                    else
                    {
                        Thread.Sleep(2000);
                    }

                    WaitForData();
                }
            }
            catch (Exception ex)
            {
                OnFailed(ex, "Error occurred while trying to read from bluetooth stream.");
            }
        }

        #endregion

        #region Discovery

        /// <summary>
        /// Performs nearby Bluetooth device discovery.
        /// </summary>
        /// <param name="maxDevices">The maximum devices to discover.</param>
        public static List<BluetoothDevice> DiscoverDevices(int? maxDevices = null)
        {
            List<BluetoothDevice> devices = new List<BluetoothDevice>();

            BluetoothClient client = new BluetoothClient();

            IReadOnlyCollection<BluetoothDeviceInfo> items = null;

            if (maxDevices == null)
            {
                items = client.DiscoverDevices();
            }
            else
            {
                items = client.DiscoverDevices(maxDevices.Value);
            }

            foreach (var item in items)
            {
                BluetoothDevice device = new BluetoothDevice(item);
                devices.Add(device);
            }

            return devices;
        }

        /// <summary>
        /// Performs nearby Bluetooth device discovery.
        /// </summary>
        /// <param name="maxDevices">The maximum devices to discover.</param>
        public static Task<List<BluetoothDevice>> DiscoverDevicesAsync(int? maxDevices = null)
        {
            return Task.Factory.StartNew<List<BluetoothDevice>>(() =>
            {
                return DiscoverDevices(maxDevices);
            });
        }

        /// <summary>
        /// Returns a list of paired/bonded Bluetooth devices.
        /// </summary>
        public static List<BluetoothDevice> GetPairedDevices()
        {
            List<BluetoothDevice> devices = new List<BluetoothDevice>();

            BluetoothClient client = new BluetoothClient();

            var items = client.PairedDevices.ToList();

            foreach (var item in items)
            {
                BluetoothDevice device = new BluetoothDevice(item);
                devices.Add(device);
            }

            return devices;
        }

        /// <summary>
        /// Returns a list of paired/bonded Bluetooth devices.
        /// </summary>
        public static Task<List<BluetoothDevice>> GetPairedDevicesAsync()
        {
            return Task.Factory.StartNew<List<BluetoothDevice>>(() =>
            {
                return GetPairedDevices();
            });
        }

        /// <summary>
        /// Invokes a Platform specific Bluetooth device selection dialog.
        /// </summary>
        public static async Task<BluetoothDevice> SelectDeviceAsync()
        {
            var pick = new BluetoothDevicePicker();
            var device = await pick.PickSingleDeviceAsync();
            return new BluetoothDevice(device);
        }

        #endregion

    }
}
