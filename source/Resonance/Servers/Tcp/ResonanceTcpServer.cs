using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Resonance.Servers.Tcp
{
    /// <summary>
    /// Represents a TCP/IP listener wrapper.
    /// </summary>
    public class ResonanceTcpServer : ResonanceObject, IDisposable
    {
        private TcpListener _listener;

        #region Events

        /// <summary>
        /// Occurs when a new <see cref="TcpClient"/> has connected.
        /// </summary>
        public event EventHandler<ResonanceTcpServerClientConnectedEventArgs> ClientConnected;

        #endregion

        #region Properties

        /// <summary>
        /// The Port that is used to listen to incoming connections.
        /// </summary>
        public int Port { get; private set; }

        /// <summary>
        /// Returns true if the Server instance is running.
        /// </summary>
        public bool IsStarted { get; private set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new Server instance.
        /// </summary>
        /// <param name="port">The port number that is used to listen for incoming connections.</param>
        public ResonanceTcpServer(int port)
        {
            Port = port;
        }
        #endregion

        #region Public Methods

        /// <summary>
        /// Start Listening for incoming connections.
        /// </summary>
        public void Start()
        {
            if (!IsStarted)
            {
                _listener = new TcpListener(System.Net.IPAddress.Any, Port);
                _listener.ExclusiveAddressUse = false;
                _listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _listener.Start();
                IsStarted = true;
                Log.Info($"TCP server started on port {Port}.");
                WaitForConnection();
            }
        }
        /// <summary>
        /// Stop listening for incoming connections.
        /// </summary>
        public void Stop()
        {
            if (IsStarted)
            {
                _listener.Stop();
                IsStarted = false;
                Log.Info($"TCP server stopped on port {Port}.");
            }
        }

        #endregion

        #region Incoming Connections Methods

        private void WaitForConnection()
        {
            _listener.BeginAcceptTcpClient(new AsyncCallback(ConnectionHandler), null);
        }

        private void ConnectionHandler(IAsyncResult ar)
        {
            if (IsStarted)
            {
                try
                {
                    OnClientConnected(_listener.EndAcceptTcpClient(ar));
                    WaitForConnection();
                }
                catch (ObjectDisposedException)
                {
                    //Ignore..
                }
            }
        }

        #endregion

        #region Virtual Methods

        /// <summary>
        /// Called when a new <see cref="TcpClient"/> has connected.
        /// </summary>
        /// <param name="tcpClient">The tcp client.</param>
        protected virtual void OnClientConnected(TcpClient tcpClient)
        {
            ClientConnected?.Invoke(this, new ResonanceTcpServerClientConnectedEventArgs(tcpClient));
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Stop();
        }

        #endregion
    }
}
