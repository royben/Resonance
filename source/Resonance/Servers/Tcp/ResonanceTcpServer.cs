using Microsoft.Extensions.Logging;
using Resonance.Adapters.Tcp;
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
    public class ResonanceTcpServer : ResonanceObject, IResonanceListeningServer<TcpAdapter>
    {
        private TcpListener _listener;

        #region Events

        public event EventHandler<ResonanceListeningServerConnectionRequestEventArgs<TcpAdapter>> ConnectionRequest;

        #endregion

        #region Properties

        /// <summary>
        /// The Port that is used to listen to incoming connections.
        /// </summary>
        public int Port { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this server is currently listening for incoming connections.
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
        /// Start listening for incoming connections.
        /// </summary>
        /// <returns></returns>
        public Task StartAsync()
        {
            return Task.Factory.StartNew(() =>
            {
                if (!IsStarted)
                {
                    _listener = new TcpListener(System.Net.IPAddress.Any, Port);
                    _listener.ExclusiveAddressUse = false;
                    _listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    _listener.Start();
                    IsStarted = true;
                    Logger.LogInformation($"TCP server started on port {Port}.");
                    WaitForConnection();
                }
            });
        }

        /// <summary>
        /// Stop listening for incoming connections.
        /// </summary>
        /// <returns></returns>
        public Task StopAsync()
        {
            return Task.Factory.StartNew(() =>
            {
                if (IsStarted)
                {
                    _listener.Stop();
                    IsStarted = false;
                    Logger.LogInformation($"TCP server stopped on port {Port}.");
                }
            });
        }

        /// <summary>
        /// Start listening for incoming connections.
        /// </summary>
        public void Start()
        {
            StartAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Stop listening for incoming connections.
        /// </summary>
        public void Stop()
        {
            StopAsync().GetAwaiter().GetResult();
        }

        #endregion

        #region Incoming Connections Methods

        private void WaitForConnection()
        {
            _listener.BeginAcceptTcpClient(new AsyncCallback(ConnectionHandler), null);
        }

        private void ConnectionHandler(IAsyncResult ar)
        {
            // This runs on a thread pool thread, so anything escaping it is an unhandled
            // exception that terminates the process. Stop() can run between the IsStarted
            // check and the listener calls below, in which case TcpListener throws
            // InvalidOperationException ("Not listening") rather than ObjectDisposedException.
            if (!IsStarted) return;

            TcpClient client;

            try
            {
                client = _listener.EndAcceptTcpClient(ar);
            }
            catch (ObjectDisposedException)
            {
                return; //The listener was disposed while accepting.
            }
            catch (InvalidOperationException)
            {
                return; //The listener was stopped while accepting.
            }
            catch (SocketException)
            {
                return; //The accept was aborted.
            }

            OnConnectionRequest(client);

            //Re-check: the server may have been stopped while the event handler ran.
            if (!IsStarted) return;

            try
            {
                WaitForConnection();
            }
            catch (ObjectDisposedException)
            {
                //Ignore..
            }
            catch (InvalidOperationException)
            {
                //Ignore..
            }
        }

        #endregion

        #region Virtual Methods

        /// <summary>
        /// Called when a new tcp client has connected.
        /// </summary>
        /// <param name="tcpClient">The TCP client.</param>
        protected virtual void OnConnectionRequest(TcpClient tcpClient)
        {
            ConnectionRequest?.Invoke(this, new ResonanceListeningServerConnectionRequestEventArgs<TcpAdapter>(() => 
            {
                return new TcpAdapter(tcpClient);
            }, () => 
            {
                tcpClient.Dispose();
            }));
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            DisposeAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Disposes component resources asynchronously.
        /// </summary>
        /// <returns></returns>
        public Task DisposeAsync()
        {
            return StopAsync();
        }

        #endregion

        #region ToString

        public override string ToString()
        {
            return this.GetType().Name;
        }

        #endregion
    }
}
