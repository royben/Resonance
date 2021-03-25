using Resonance.ExtensionMethods;
using Resonance.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private bool _initializedFromConstructor;
        private TcpClient _socket;
        private byte[] _size_buffer;

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
        public TcpAdapter(String address, int port) : this()
        {
            Address = address;
            Port = port;
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

        #region Pull Thread

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
                OnFailed(ex);
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
                OnFailed(ex);
            }
        }

        #endregion

        #region Private Methods

        private void SetSocketProperties()
        {
            _socket.SendBufferSize = 1024;
            _socket.ReceiveBufferSize = 1024;
            _socket.NoDelay = true;
            _socket.Client.NoDelay = true;
        }

        #endregion

        #region Override Methods

        protected override Task OnConnect()
        {
            return Task.Factory.StartNew(() =>
            {
                try
                {
                    if (State != ResonanceComponentState.Connected)
                    {
                        if (!_initializedFromConstructor)
                        {
                            _socket = new TcpClient(Address, Port);
                            SetSocketProperties();
                        }

                        LogManager.Log($"{this}: Connected...");

                        State = ResonanceComponentState.Connected;

                        Task.Factory.StartNew(() =>
                        {
                            WaitForData();
                        }, TaskCreationOptions.LongRunning);
                    }
                }
                catch (Exception ex)
                {
                    throw LogManager.Log(ex, $"Could not connect the TCP adapter ({Address}).");
                }
            });
        }

        protected override Task OnDisconnect()
        {
            return Task.Factory.StartNew((Action)(() =>
            {
                try
                {
                    if (State == ResonanceComponentState.Connected)
                    {
                        State = ResonanceComponentState.Disconnected;
                        _socket.Close();
                        LogManager.Log($"{this} Disconnected.");
                    }
                }
                catch (Exception ex)
                {
                    LogManager.Log(ex, $"{this} Could not disconnect the adapter.");
                }
            }));
        }

        protected override void OnWrite(byte[] data)
        {
            try
            {
                data = PrependDataSize(data);
                _socket.GetStream().Write(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                OnFailed(LogManager.Log(ex, $"{this}: Error writing to socket stream."));
            }
        }

        #endregion
    }
}
