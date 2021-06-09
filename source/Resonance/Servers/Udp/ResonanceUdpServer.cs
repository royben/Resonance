using Microsoft.Extensions.Logging;
using Resonance.Adapters.Udp;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Resonance.Servers.Udp
{
    /// <summary>
    /// Represents a UDP server that mimics the behavior of a TCP server for UDP adapters.
    /// </summary>
    /// <seealso cref="Resonance.ResonanceObject" />
    /// <seealso cref="Resonance.IResonanceListeningServer{Resonance.Adapters.Udp.UdpAdapter}" />
    public class ResonanceUdpServer : ResonanceObject, IResonanceListeningServer<UdpAdapter>
    {
        private UdpClient _server;
        private Thread _connectionThread;

        /// <summary>
        /// Occurs when a new connection request is available.
        /// </summary>
        public event EventHandler<ResonanceListeningServerConnectionRequestEventArgs<UdpAdapter>> ConnectionRequest;

        /// <summary>
        /// Gets a value indicating whether this server is currently listening for incoming connections.
        /// </summary>
        public bool IsStarted { get; private set; }

        /// <summary>
        /// Gets the port the server is listening for incoming connection on.
        /// </summary>
        public int Port { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResonanceUdpServer"/> class.
        /// </summary>
        /// <param name="port">The port to listen for incoming connections.</param>
        public ResonanceUdpServer(int port)
        {
            Port = port;
        }

        /// <summary>
        /// Start listening for incoming connections.
        /// </summary>
        public void Start()
        {
            StartAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Start listening for incoming connections.
        /// </summary>
        /// <returns></returns>
        public Task StartAsync()
        {
            if (!IsStarted)
            {
                _server = new UdpClient(new IPEndPoint(IPAddress.Any, Port));
                _connectionThread = new Thread(ConnectionThreadMethod);
                _connectionThread.IsBackground = true;
                _connectionThread.Name = $"{this} pull thread thread";
                IsStarted = true;

                Logger.LogInformation($"UDP server started on port {Port}.");

                _connectionThread.Start();
            }

            return Task.FromResult(true);
        }

        /// <summary>
        /// Stop listening for incoming connections.
        /// </summary>
        public void Stop()
        {
            StopAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Stop listening for incoming connections.
        /// </summary>
        /// <returns></returns>
        public Task StopAsync()
        {
            if (IsStarted)
            {
                IsStarted = false;
                _server.Dispose();
                Logger.LogInformation($"UDP server stopped on port {Port}.");
            }

            return Task.FromResult(true);
        }

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

        private void ConnectionThreadMethod()
        {
            while (IsStarted)
            {
                try
                {
                    IPEndPoint remoteEndPoint = null;
                    byte[] requestData = _server.Receive(ref remoteEndPoint);
                    if (!IsStarted) return;

                    String requestString = Encoding.ASCII.GetString(requestData);

                    if (requestString == UdpAdapter.CONNECTION_REQUEST_STRING)
                    {
                        String address = remoteEndPoint.Address.ToString();
                        int port = remoteEndPoint.Port;

                        OnConnectionRequest(remoteEndPoint);
                    }
                }
                catch { }
            }
        }

        protected virtual void OnConnectionRequest(IPEndPoint remoteEndpoint)
        {
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, 0);

            UdpClient udpClient = new UdpClient(localEndPoint);

            ConnectionRequest?.Invoke(this, new ResonanceListeningServerConnectionRequestEventArgs<UdpAdapter>(() =>
            {
                byte[] confirmData = Encoding.ASCII.GetBytes(UdpAdapter.CONNECTION_RESPONSE_STRING_CONFIRMED);
                udpClient.Send(confirmData, confirmData.Length, remoteEndpoint);
                return new UdpAdapter(udpClient, localEndPoint, remoteEndpoint);
            }, () =>
            {
                byte[] declineData = Encoding.ASCII.GetBytes(UdpAdapter.CONNECTION_RESPONSE_STRING_DECLINED);
                udpClient.Send(declineData, declineData.Length, remoteEndpoint);
                udpClient.Dispose();
            }));
        }

        public override string ToString()
        {
            return this.GetType().Name;
        }
    }
}
