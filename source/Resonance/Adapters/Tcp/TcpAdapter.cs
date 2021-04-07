using Resonance.ExtensionMethods;
using Resonance.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Resonance.Adapters.Tcp
{
    /// <summary>
    /// Represents a TCP/IP Resonance adapter.
    /// </summary>
    public class TcpAdapter : ResonanceAdapter
    {
        private readonly bool _initializedFromConstructor;
        private TcpClient _socket;
        private byte[] _size_buffer;
        private readonly bool _noDelay;

        /// <summary>
        /// The maximum socket buffer size.
        /// </summary>
        protected const int MAX_BUFFER_SIZE = 1024; //10 MB.

        #region Properties

        /// <summary>
        /// Gets or sets the host IP address.
        /// </summary>
        public String Address { get; set; }

        /// <summary>
        /// Gets or sets the host port.
        /// </summary>
        public int Port { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="TcpAdapter"/> class.
        /// </summary>
        public TcpAdapter()
        {
            Address = "127.0.0.1";
            Port = 9999;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TcpAdapter"/> class.
        /// </summary>
        /// <param name="address">The host IP address.</param>
        /// <param name="port">The host port.</param>
        /// <param name="noDelay">Disable Nagle's algorithm.</param>
        public TcpAdapter(String address, int port, bool noDelay = false) : this()
        {
            Address = address;
            Port = port;
            _noDelay = noDelay;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TcpAdapter"/> class.
        /// </summary>
        /// <param name="tcpClient">Existing <see cref="TcpClient"/>.</param>
        public TcpAdapter(TcpClient tcpClient) : this()
        {
            _initializedFromConstructor = true;
            _socket = tcpClient;
            Address = tcpClient.GetIPAddress().ToStringOrEmpty();
            SetSocketProperties();
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
                    _socket.GetStream().BeginRead(_size_buffer, 0, _size_buffer.Length, EndReading, _socket.GetStream());

                }
            }
            catch (Exception ex)
            {
                OnFailed(ex, "Error occurred while trying to read from network stream.");
            }
        }

        private void EndReading(IAsyncResult ar)
        {
            try
            {
                if (State == ResonanceComponentState.Connected)
                {
                    _socket.GetStream().EndRead(ar);

                    int expectedSize = BitConverter.ToInt32(_size_buffer, 0);

                    if (expectedSize > 0)
                    {
                        byte[] data = new byte[expectedSize];
                        int read = 0;

                        while (read < expectedSize)
                        {
                            read += _socket.GetStream().Read(data, read, Math.Min(_socket.Available, expectedSize - read));

                            if (State != ResonanceComponentState.Connected)
                            {
                                break;
                            }
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
                OnFailed(ex, "Error occurred while trying to read from network stream.");
            }
        }

        #endregion

        #region Private Methods

        private void SetSocketProperties()
        {
            _socket.SendBufferSize = 1024;
            _socket.ReceiveBufferSize = 1024;
            _socket.NoDelay = _noDelay;
            _socket.Client.NoDelay = _noDelay;
        }

        #endregion

        #region Connect / Disconnect / Write

        protected override Task OnConnect()
        {
            return Task.Factory.StartNew(() =>
            {
                if (!_initializedFromConstructor)
                {
                    _socket = new TcpClient(Address, Port);
                    SetSocketProperties();
                }
                else if (!_socket.Connected)
                {
                    _socket.Connect(Address, Port);
                }
                else
                {
                    Address = _socket.GetIPAddress().ToStringOrEmpty();
                    Port = _socket.GetPort();
                }

                State = ResonanceComponentState.Connected;

                Task.Factory.StartNew(() =>
                {
                    WaitForData();
                }, TaskCreationOptions.LongRunning);
            });
        }

        protected override Task OnDisconnect()
        {
            return Task.Factory.StartNew((Action)(() =>
            {
                State = ResonanceComponentState.Disconnected;
                _socket.Close();
            }));
        }

        protected override void OnWrite(byte[] data)
        {
            data = PrependDataSize(data);
            _socket.GetStream().Write(data, 0, data.Length);
        }

        #endregion

        #region ToString

        public override string ToString()
        {
            return $"{base.ToString()} ({Address}/{Port})";
        }

        #endregion

        #region Static Methods

        /// <summary>
        /// Gets the machine's LAN IP address.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception">No network adapters with an IPv4 address in the system!</exception>
        public static String GetLocalIPAddress()
        {
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                var addr = ni.GetIPProperties().GatewayAddresses.FirstOrDefault();
                if (addr != null && !addr.Address.ToString().Equals("0.0.0.0"))
                {
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 || ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                    {
                        foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                        {
                            if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                            {
                                return ip.Address.ToString();
                            }
                        }
                    }
                }
            }

            throw new Exception("Could not retrieve this machine local IP address.");
        }

        #endregion
    }
}
