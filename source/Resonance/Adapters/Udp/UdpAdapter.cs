using Resonance.Tokens;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Resonance.Adapters.Udp
{
    /// <summary>
    /// Represents a UDP Resonance adapter.
    /// </summary>
    /// <seealso cref="Resonance.ResonanceAdapter" />
    public class UdpAdapter : ResonanceAdapter
    {
        private UdpClient _socket;
        private byte[] _tokenData;
        private Thread _pullThread;

        #region Properties

        /// <summary>
        /// Gets the remote end point to connect to.
        /// </summary>
        public IPEndPoint RemoteEndPoint { get; set; }

        /// <summary>
        /// Gets or sets the local end point to bind to.
        /// </summary>
        public IPEndPoint LocalEndPoint { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to prevent packs sent by this adapter to be received by this adapter even when sent to the localhost address.
        /// This is done by attaching a 22 bytes adapter identifier to each message.
        /// For this to work properly, all adapters participating should use the same setting.
        /// </summary>
        public bool PreventLoopback { get; set; }

        /// <summary>
        /// Gets the adapter identifier used to identify this adapter when using <see cref="PreventLoopback"/>.
        /// </summary>
        public String AdapterIdentifier { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="UdpAdapter"/> class.
        /// </summary>
        public UdpAdapter()
        {
            AdapterIdentifier = new ShortGuidGenerator().GenerateToken(null);
            _tokenData = Encoding.ASCII.GetBytes(AdapterIdentifier);

            _socket = new UdpClient();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UdpAdapter"/> class.
        /// </summary>
        public UdpAdapter(IPEndPoint localEndPoint, IPEndPoint remoteEndPoint) : this()
        {
            LocalEndPoint = localEndPoint;
            RemoteEndPoint = remoteEndPoint;
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
                _socket.EnableBroadcast = true;
                _socket.ExclusiveAddressUse = false;
                _socket.MulticastLoopback = false;
                _socket.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _socket.Client.Bind(LocalEndPoint);

                State = ResonanceComponentState.Connected;

                _pullThread = new Thread(PullThreadMethod);
                _pullThread.IsBackground = true;
                _pullThread.Name = $"{this} pull thread";
                _pullThread.Start();
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
                State = ResonanceComponentState.Disconnected;
                _socket.Close();
            }));
        }

        /// <summary>
        /// Called when the adapter is writing.
        /// </summary>
        /// <param name="data">The data.</param>
        protected override void OnWrite(byte[] data)
        {
            if (PreventLoopback)
            {
                data = _tokenData.Concat(data).ToArray();
            }

            _socket.Send(data, data.Length, RemoteEndPoint);
        }

        #endregion

        #region Pull Thread

        private void PullThreadMethod()
        {
            try
            {
                while (State == ResonanceComponentState.Connected)
                {
                    try
                    {
                        var remoteEndPoint = RemoteEndPoint;
                        byte[] data = _socket.Receive(ref remoteEndPoint);

                        if (PreventLoopback)
                        {
                            ArraySegment<byte> tokenData = new ArraySegment<byte>(data, 0, _tokenData.Length);
                            ArraySegment<byte> restOfData = new ArraySegment<byte>(data, _tokenData.Length - 1, data.Length - _tokenData.Length);
                            data = restOfData.ToArray();

                            if (tokenData.SequenceEqual(_tokenData))
                            {
                                continue;
                            }
                        }

                        OnDataAvailable(data);
                    }
                    catch (Exception ex)
                    {
                        if (ex.Message != "A blocking operation was interrupted by a call to WSACancelBlockingCall")
                        {
                            Debugger.Break();
                        }
                        else
                        {
                            throw ex;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                OnFailed(ex, "Error occurred while trying to read from network stream.");
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
            if (RemoteEndPoint != null)
            {
                return $"{base.ToString()} ({RemoteEndPoint.Address.ToString()}/{RemoteEndPoint.Port})";
            }
            else
            {
                return $"{base.ToString()} (no remote endpoint)";
            }
        }

        #endregion
    }
}
