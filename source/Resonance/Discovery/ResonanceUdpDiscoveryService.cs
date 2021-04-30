using Microsoft.Extensions.Logging;
using Resonance.ExtensionMethods;
using Resonance.Servers.Tcp;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Resonance.Discovery
{
    /// <summary>
    /// Represents a UDP/TCP discovery serice broadcasting a discovery information message.
    /// Once remote clients detects the discovery message they can validate the existence by connecting and disconnecting via TCP on the same port.</summary>
    /// <typeparam name="TDiscoveryInfo">The type of the discovery information.</typeparam>
    /// <typeparam name="TEncoder">The type of the encoder.</typeparam>
    /// <seealso cref="Resonance.ResonanceObject" />
    /// <seealso cref="Resonance.Discovery.IResonanceDiscoveryService{TDiscoveryInfo, TEncoder}" />
    public class ResonanceUdpDiscoveryService<TDiscoveryInfo, TEncoder> : ResonanceObject, IResonanceDiscoveryService<TDiscoveryInfo, TEncoder> where TDiscoveryInfo : class, new() where TEncoder : IResonanceEncoder, new()
    {
        private Timer _timer;
        private ResonanceTcpServer _tcpValidationServer;
        private int _componentCounter;

        /// <summary>
        /// Occurs before broadcasting the discovery message and gives a chance to modify the message.
        /// </summary>
        public event EventHandler<TDiscoveryInfo> BeforeBroadcasting;

        /// <summary>
        /// Gets the current discovery information instance.
        /// </summary>
        public TDiscoveryInfo DiscoveryInfo { get; set; }

        /// <summary>
        /// Gets or sets the interval in which the discovery message will be sent.
        /// The default if 5 seconds.
        /// </summary>
        public TimeSpan Interval { get; set; }

        /// <summary>
        /// Gets a value indicating whether this discovery service has been started.
        /// </summary>
        public bool IsStarted { get; private set; }

        /// <summary>
        /// Gets the UDP/TCP port number to operate on.
        /// The default is 2021.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Gets the encoder that is used to encode the discovery information message.
        /// </summary>
        public TEncoder Encoder { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResonanceUdpDiscoveryService{TDiscoveryInfo, TEncoder}"/> class.
        /// </summary>
        public ResonanceUdpDiscoveryService()
        {
            _componentCounter = ResonanceComponentCounterManager.Default.GetIncrement(this);
            Port = 2021;
            Interval = TimeSpan.FromSeconds(5);
            DiscoveryInfo = Activator.CreateInstance<TDiscoveryInfo>();
            Encoder = Activator.CreateInstance<TEncoder>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResonanceUdpDiscoveryService{TDiscoveryInfo, TEncoder}"/> class.
        /// </summary>
        /// <param name="discoveryInfo">The discovery information.</param>
        /// <param name="port">The udp/tcp port number to operate on.</param>
        public ResonanceUdpDiscoveryService(TDiscoveryInfo discoveryInfo, int port) : this()
        {
            DiscoveryInfo = discoveryInfo;
            Port = port;
        }

        /// <summary>
        /// Starts the discovery service.
        /// </summary>
        public async Task Start()
        {
            if (!IsStarted)
            {
                Logger.LogInformation("Starting...");
                Logger.LogDebug("Discovery Info: {@DiscoveryInfo}", DiscoveryInfo);

                _tcpValidationServer = new ResonanceTcpServer(Port);
                _tcpValidationServer.ConnectionRequest += (x, e) =>
                {
                    e.Decline();
                };

                await _tcpValidationServer.Start();

                _timer = new Timer();
                _timer.Interval = Interval.TotalMilliseconds;
                _timer.Elapsed += (_, __) => BroadcastDiscoveryPacket();
                _timer.Enabled = true;
                _timer.Start();

                IsStarted = true;
                Logger.LogInformation("Started.");
            }
        }

        /// <summary>
        /// Stops the discovery service.
        /// </summary>
        public async Task Stop()
        {
            if (IsStarted)
            {
                Logger.LogInformation("Stopping...");

                await _tcpValidationServer.Stop();

                //Transmit the discovery packet one more time so clients can tell that we have disconnected.
                BroadcastDiscoveryPacket();

                _timer.Stop();
                IsStarted = false;
                Logger.LogInformation("Stopped.");
            }
        }

        private void BroadcastDiscoveryPacket()
        {
            try
            {
                Logger.LogDebug("Broadcasting discovery message...");

                UdpClient client = new UdpClient();
                client.EnableBroadcast = true;

                IPEndPoint endPoint = new IPEndPoint(IPAddress.Broadcast, Port);

                BeforeBroadcasting?.Invoke(this, DiscoveryInfo);

                byte[] data = Encoder.Encode(new ResonanceEncodingInformation()
                {
                    Token = Guid.NewGuid().ToString(),
                    Type = ResonanceTranscodingInformationType.Request,
                    Message = DiscoveryInfo
                });

                client.Send(data, data.Length, endPoint);

                client.Close();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error broadcasting discovery message.");
            }
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
            return Stop();
        }

        /// <summary>
        /// Gets the string representation of this instance.
        /// </summary>
        public override string ToString()
        {
            return $"UDP Discovery Service {_componentCounter}";
        }
    }
}
