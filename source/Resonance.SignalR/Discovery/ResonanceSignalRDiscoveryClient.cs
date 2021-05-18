using Resonance.Discovery;
using Resonance.SignalR;
using Resonance.SignalR.Clients;
using Resonance.Transcoding.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Resonance.SignalR.Discovery
{
    /// <summary>
    /// Represents a SignalR discovery client capable of receiving and notifying about available remote services.
    /// </summary>
    /// <typeparam name="TReportedServiceInformation">The type of the reported service information.</typeparam>
    /// <typeparam name="TCredentials">The type of the credentials.</typeparam>
    public class ResonanceSignalRDiscoveryClient<TReportedServiceInformation, TCredentials> : IResonanceDiscoveryClient<TReportedServiceInformation, ResonanceSignalRDiscoveredService<TReportedServiceInformation>> where TReportedServiceInformation : class, IResonanceServiceInformation, new()
    {
        private ISignalRClient _client;
        private List<ResonanceSignalRDiscoveredService<TReportedServiceInformation>> _discoveredServices;

        /// <summary>
        /// Occurs when the discovery client has disconnected due to some connection error.
        /// </summary>
        public event EventHandler<ResonanceExceptionEventArgs> Disconnected;

        /// <summary>
        /// Gets the credentials used to authenticate this discovery client with the remote hub.
        /// </summary>
        public TCredentials Credentials { get; set; }

        /// <summary>
        /// Gets the SignalR client mode (core/legacy).
        /// </summary>
        public SignalRMode Mode { get; private set; }

        /// <summary>
        /// Gets the remote hub URL.
        /// </summary>
        public String HubUrl { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to enable auto reconnection when connection has lost.
        /// </summary>
        public bool EnableAutoReconnection { get; set; }

        /// <summary>
        /// Gets a value indicating whether this client has started.
        /// </summary>
        public bool IsStarted { get; private set; }

        /// <summary>
        /// Occurs when a matching service has been discovered.
        /// </summary>
        public event EventHandler<ResonanceDiscoveredServiceEventArgs<ResonanceSignalRDiscoveredService<TReportedServiceInformation>, TReportedServiceInformation>> ServiceDiscovered;

        /// <summary>
        /// Occurs when a discovered service is no longer responding.
        /// </summary>
        public event EventHandler<ResonanceDiscoveredServiceEventArgs<ResonanceSignalRDiscoveredService<TReportedServiceInformation>, TReportedServiceInformation>> ServiceLost;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResonanceSignalRDiscoveryClient{TReportedServiceInformation, TCredentials}"/> class.
        /// </summary>
        /// <param name="hubUrl">The remote hub URL.</param>
        /// <param name="mode">The SignalR client mode (core/legacy).</param>
        /// <param name="credentials">The credentials used to authenticate this discovery client with the remote hub.</param>
        public ResonanceSignalRDiscoveryClient(String hubUrl, SignalRMode mode, TCredentials credentials)
        {
            HubUrl = hubUrl;
            Mode = mode;
            Credentials = credentials;
            EnableAutoReconnection = true;
        }

        /// <summary>
        /// Asynchronous method for collecting discovered services within the given duration.
        /// </summary>
        /// <param name="maxDuration">The maximum duration to perform the scan.</param>
        /// <param name="maxServices">Drop the scanning after the maximum services discovered.</param>
        /// <returns></returns>
        public List<ResonanceSignalRDiscoveredService<TReportedServiceInformation>> Discover(TimeSpan maxDuration, int? maxServices = null)
        {
            return DiscoverAsync(maxDuration, maxServices).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Asynchronous method for collecting discovered services within the given duration.
        /// </summary>
        /// <param name="maxDuration">The maximum duration to perform the scan.</param>
        /// <param name="maxServices">Drop the scanning after the maximum services discovered.</param>
        /// <returns></returns>
        public async Task<List<ResonanceSignalRDiscoveredService<TReportedServiceInformation>>> DiscoverAsync(TimeSpan maxDuration, int? maxServices = null)
        {
            await StartAsync();

            return await Task.Factory.StartNew<List<ResonanceSignalRDiscoveredService<TReportedServiceInformation>>>(() =>
            {
                DateTime startTime = DateTime.Now;

                while (DateTime.Now < startTime + maxDuration)
                {
                    Thread.Sleep(10);

                    if (maxServices != null && _discoveredServices.Count >= maxServices.Value)
                    {
                        break;
                    }
                }

                if (maxServices != null)
                {
                    return _discoveredServices.Take(maxServices.Value).ToList();
                }
                else
                {
                    return _discoveredServices.ToList();
                }
            });
        }

        /// <summary>
        /// Start discovering.
        /// </summary>
        public async Task StartAsync()
        {
            if (!IsStarted)
            {
                _discoveredServices = new List<ResonanceSignalRDiscoveredService<TReportedServiceInformation>>();

                _client = SignalRClientFactory.Default.Create(Mode, HubUrl);
                _client.EnableAutoReconnection = EnableAutoReconnection;
                _client.Error += OnDisconnected;
                await _client.StartAsync();

                await _client.InvokeAsync(ResonanceHubMethods.RegisterDiscoveryClient, Credentials);
                var services = await _client.InvokeAsync<List<TReportedServiceInformation>>(ResonanceHubMethods.GetAvailableServices, Credentials);

                foreach (var serviceInfo in services)
                {
                    OnServiceRegistered(serviceInfo);
                }

                _client.On<TReportedServiceInformation>(ResonanceHubMethods.ServiceRegistered, OnServiceRegistered);
                _client.On<TReportedServiceInformation>(ResonanceHubMethods.ServiceUnRegistered, OnServiceUnregistered);
                IsStarted = true;
            }
        }

        /// <summary>
        /// Start discovering.
        /// </summary>
        public void Start()
        {
            StartAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Stop discovering.
        /// </summary>
        public async Task StopAsync()
        {
            if (IsStarted)
            {
                try
                {
                    await _client.DisposeAsync();
                }
                catch { }
                IsStarted = false;
            }
        }

        /// <summary>
        /// Start discovering.
        /// </summary>
        public void Stop()
        {
            StopAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Called when a new service has been discovered.
        /// </summary>
        /// <param name="discoveryInfo">The discovery information.</param>
        protected virtual void OnServiceRegistered(TReportedServiceInformation discoveryInfo)
        {
            if (!_discoveredServices.Exists(x => x.DiscoveryInfo.ServiceId == discoveryInfo.ServiceId))
            {
                _discoveredServices.Add(new ResonanceSignalRDiscoveredService<TReportedServiceInformation>(discoveryInfo));
                ServiceDiscovered?.Invoke(this, new ResonanceDiscoveredServiceEventArgs<ResonanceSignalRDiscoveredService<TReportedServiceInformation>, TReportedServiceInformation>(new ResonanceSignalRDiscoveredService<TReportedServiceInformation>(discoveryInfo)));
            }
        }

        /// <summary>
        /// Called when a service has been lost.
        /// </summary>
        /// <param name="discoveryInfo">The discovery information.</param>
        protected virtual void OnServiceUnregistered(TReportedServiceInformation discoveryInfo)
        {
            var existingService = _discoveredServices.FirstOrDefault(x => x.DiscoveryInfo.ServiceId == discoveryInfo.ServiceId);
            if (existingService != null)
            {
                _discoveredServices.Remove(existingService);
                ServiceLost?.Invoke(this, new ResonanceDiscoveredServiceEventArgs<ResonanceSignalRDiscoveredService<TReportedServiceInformation>, TReportedServiceInformation>(existingService));
            }
        }

        /// <summary>
        /// Called when the internal SignalR client has disconnected due to some connection error.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="ResonanceExceptionEventArgs"/> instance containing the event data.</param>
        protected virtual void OnDisconnected(object sender, ResonanceExceptionEventArgs e)
        {
            Disconnected?.Invoke(this, e);
        }

        /// <summary>
        /// Disposes component resources asynchronously.
        /// </summary>
        public async Task DisposeAsync()
        {
            if (IsStarted)
            {
                await StopAsync();
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            DisposeAsync().GetAwaiter().GetResult();
        }
    }
}
