using Resonance.Threading;
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
        public static String CONNECTION_REQUEST_STRING = "RESONANCE_CONNECTION_REQUEST";
        public static String CONNECTION_RESPONSE_STRING_CONFIRMED = "RESONANCE_CONNECTION_RESPONSE_CONFIRMED";
        public static String CONNECTION_RESPONSE_STRING_DECLINED = "RESONANCE_CONNECTION_RESPONSE_DECLINED";
        private bool _isServerClient;

        #region Properties

        /// <summary>
        /// Gets or sets the connection timeout.
        /// </summary>
        public TimeSpan ConnectionTimeout { get; set; }

        /// <summary>
        /// Gets the adapter UDP remote end point for outgoing data.
        /// </summary>
        public IPEndPoint RemoteEndPoint { get; private set; }

        /// <summary>
        /// Gets the adapter UDP local end point for incoming data.
        /// </summary>
        public IPEndPoint LocalEndPoint { get; private set; }

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
        /// Prevents a default instance of the <see cref="UdpAdapter"/> class from being created.
        /// </summary>
        private UdpAdapter()
        {
            ConnectionTimeout = TimeSpan.FromSeconds(5);
            AdapterIdentifier = new ShortGuidGenerator().GenerateToken(null);
            _tokenData = Encoding.ASCII.GetBytes(AdapterIdentifier);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UdpAdapter"/> class.
        /// </summary>
        /// <param name="client">The client.</param>
        /// <param name="localEndPoint">The local end point.</param>
        /// <param name="remoteEndPoint">The remote end point.</param>
        public UdpAdapter(UdpClient client, IPEndPoint localEndPoint, IPEndPoint remoteEndPoint) : this()
        {
            _socket = client;
            RemoteEndPoint = remoteEndPoint;
            LocalEndPoint = localEndPoint;
            _isServerClient = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UdpAdapter"/> class.
        /// </summary>
        /// <param name="localEndPoint">The local end point.</param>
        /// <param name="remoteEndPoint">The remote end point.</param>
        public UdpAdapter(IPEndPoint localEndPoint, IPEndPoint remoteEndPoint) : this()
        {
            RemoteEndPoint = remoteEndPoint;
            LocalEndPoint = localEndPoint;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UdpAdapter"/> class.
        /// </summary>
        /// <param name="remoteEndPoint">The remote end point.</param>
        public UdpAdapter(IPEndPoint remoteEndPoint) : this()
        {
            _isServerClient = true;
            RemoteEndPoint = remoteEndPoint;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UdpAdapter"/> class.
        /// </summary>
        /// <param name="serverAddress">The server address.</param>
        /// <param name="port">The port.</param>
        public UdpAdapter(String serverAddress, int port) : this(new IPEndPoint(IPAddress.Parse(serverAddress), port))
        {

        }

        #endregion

        #region Connect / Disconnect /Write

        /// <summary>
        /// Called when the adapter is connecting.
        /// </summary>
        /// <returns></returns>
        protected override Task OnConnect()
        {
            TaskCompletionSource<object> completion = new TaskCompletionSource<object>();
            bool completed = false;

            Task.Factory.StartNew(() =>
            {
                if (_socket == null)
                {
                    if (LocalEndPoint == null)
                    {
                        LocalEndPoint = new IPEndPoint(IPAddress.Any, 0);
                    }

                    if (_isServerClient)
                    {
                        _socket = new UdpClient(LocalEndPoint);
                        _socket.EnableBroadcast = true;

                        TimeoutTask.StartNew(() =>
                        {

                            if (!completed)
                            {
                                completed = true;
                                completion.SetException(new TimeoutException("Could not connect within the given timeout."));
                            }

                        }, ConnectionTimeout);

                        String response = null;

                        try
                        {
                            while (true)
                            {
                                byte[] requestData = Encoding.ASCII.GetBytes(CONNECTION_REQUEST_STRING);
                                _socket.Send(requestData, requestData.Length, RemoteEndPoint);
                                var remoteEndPoint = RemoteEndPoint;
                                byte[] responseData = _socket.Receive(ref remoteEndPoint);
                                response = Encoding.ASCII.GetString(responseData);
                                RemoteEndPoint = remoteEndPoint;

                                if (response == CONNECTION_RESPONSE_STRING_CONFIRMED || response == CONNECTION_RESPONSE_STRING_DECLINED)
                                {
                                    break;
                                }
                            }

                            if (response == CONNECTION_RESPONSE_STRING_DECLINED)
                            {
                                throw new Exception("The remote host has declined the connection request.");
                            }
                        }
                        catch (Exception ex)
                        {
                            if (!completed)
                            {
                                completed = true;
                                completion.SetException(ex);
                            }
                        }
                    }
                    else
                    {
                        _socket = new UdpClient();
                        _socket.EnableBroadcast = true;
                        _socket.ExclusiveAddressUse = false;
                        _socket.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                        _socket.Client.Bind(LocalEndPoint);
                    }
                }

                if (!completed)
                {
                    completed = true;

                    State = ResonanceComponentState.Connected;

                    _pullThread = new Thread(PullThreadMethod);
                    _pullThread.IsBackground = true;
                    _pullThread.Name = $"{this} pull thread";
                    _pullThread.Start();

                    completion.SetResult(true);
                }
            });

            return completion.Task;
        }

        /// <summary>
        /// Called when the adapter is disconnecting.
        /// </summary>
        /// <returns></returns>
        protected override Task OnDisconnect()
        {
            return Task.Factory.StartNew((Action)(() =>
            {
                _socket.Close();
                _socket = null;
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
                            ArraySegment<byte> restOfData = new ArraySegment<byte>(data, _tokenData.Length, data.Length - _tokenData.Length);
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
    }
}
