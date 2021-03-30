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
    public class ResonanceTcpServer : ResonanceObject , IDisposable
    {
        /// <summary>
        /// The TcpListener that is encapsulated behind this Server instance.
        /// </summary>
        public TcpListener Listener { get; set; }

        /// <summary>
        /// The Port that is used to listen to incoming connections.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Returns true if the Server instance is running.
        /// </summary>
        public bool IsStarted { get; private set; }

        #region Events

        /// <summary>
        /// Occurs when a new <see cref="TcpClient"/> has connected.
        /// </summary>
        public event EventHandler<ResonanceTcpServerClientConnectedEventArgs> ClientConnected;

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
                Listener = new TcpListener(System.Net.IPAddress.Any, Port);
                Listener.ExclusiveAddressUse = false;
                Listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                Listener.Start();
                IsStarted = true;
                LogManager.Log($"TCP server started on port {Port}.");
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
                Listener.Stop();
                IsStarted = false;
                LogManager.Log($"TCP server stopped on port {Port}.");
            }
        }

        #endregion

        #region Incoming Connections Methods

        private void WaitForConnection()
        {
            Listener.BeginAcceptTcpClient(new AsyncCallback(ConnectionHandler), null);
        }

        private void ConnectionHandler(IAsyncResult ar)
        {
            if (IsStarted)
            {
                try
                {
                    OnClientConnected(Listener.EndAcceptTcpClient(ar));
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
