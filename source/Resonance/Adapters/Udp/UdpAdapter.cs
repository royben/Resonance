using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.Adapters.Udp
{
    /// <summary>
    /// Represents a UDP Resonance adapter.
    /// </summary>
    /// <seealso cref="Resonance.ResonanceAdapter" />
    public class UdpAdapter : ResonanceAdapter
    {
        private bool _initializedFromConstructor;
        private UdpClient _socket;
        private IPEndPoint _endPoint;

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
        /// Initializes a new instance of the <see cref="UdpAdapter"/> class.
        /// </summary>
        public UdpAdapter()
        {
            Address = "127.0.0.1";
            Port = 9999;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UdpAdapter"/> class.
        /// </summary>
        /// <param name="address">The remote peer ip address.</param>
        /// <param name="port">The udp port.</param>
        public UdpAdapter(String address, int port) : this()
        {
            Address = address;
            Port = port;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UdpAdapter"/> class.
        /// </summary>
        /// <param name="udpClient">The UDP client.</param>
        /// <param name="endPoint">The remote end point.</param>
        public UdpAdapter(UdpClient udpClient, IPEndPoint endPoint) : this()
        {
            _initializedFromConstructor = true;
            _socket = udpClient;
            _endPoint = endPoint;
            Address = endPoint.Address.ToString();
            Port = endPoint.Port;
            SetSocketProperties();
        }

        #endregion

        #region Private Methods

        private void SetSocketProperties()
        {
            _socket.EnableBroadcast = true;
            _socket.ExclusiveAddressUse = false;
            _socket.MulticastLoopback = false;

            _socket.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _socket.Client.Bind(_endPoint);
        }

        #endregion

        #region Connect / Disconnect /Write

        /// <summary>
        /// Called when the adapter is connecting.
        /// </summary>
        /// <returns></returns>
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
                            _socket = new UdpClient();
                            _endPoint = new IPEndPoint(IPAddress.Parse(Address), Port);
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

        /// <summary>
        /// Called when the adapter is disconnecting.
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Called when the adapter is writing.
        /// </summary>
        /// <param name="data">The data.</param>
        protected override void OnWrite(byte[] data)
        {
            try
            {
                _socket.Send(data, data.Length, _endPoint);
            }
            catch (Exception ex)
            {
                OnFailed(LogManager.Log(ex, $"{this}: Error writing to socket stream."));
            }
        }

        #endregion

        #region Pull Thread

        private void WaitForData()
        {
            try
            {
                while (State == ResonanceComponentState.Connected)
                {
                    try
                    {
                        byte[] data = _socket.Receive(ref _endPoint);
                        OnDataAvailable(data);
                    }
                    catch (Exception)
                    {
                        Debugger.Break();
                    }
                }
            }
            catch (Exception ex)
            {
                OnFailed(ex);
            }
        }

        #endregion

        #region ToString

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return $"{base.ToString()} ({Address}/{Port})";
        }

        #endregion
    }
}
