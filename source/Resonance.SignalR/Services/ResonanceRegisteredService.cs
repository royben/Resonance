using Microsoft.AspNet.SignalR.Client;
using Resonance.Adapters.SignalR;
using Resonance.SignalR.Clients;
using Resonance.SignalR.Hubs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.SignalR.Services
{
    /// <summary>
    /// Represents a Resonance SignalR service.
    /// </summary>
    /// <typeparam name="TCredentials">The type of the credentials.</typeparam>
    /// <typeparam name="TResonanceServiceInformation">The type of the resonance service information.</typeparam>
    /// <typeparam name="TAdapterInformation">The type of the adapter information.</typeparam>
    /// <seealso cref="System.IDisposable" />
    public class ResonanceRegisteredService<TCredentials, TResonanceServiceInformation, TAdapterInformation> : IDisposable where TResonanceServiceInformation : IResonanceServiceInformation
    {
        private ISignalRClient _client;

        /// <summary>
        /// Occurs when a remote adapter has requested a connection.
        /// </summary>
        public event EventHandler<ConnectionRequestEventArgs<TCredentials, TAdapterInformation>> ConnectionRequest;

        /// <summary>
        /// Gets the service information.
        /// </summary>
        public TResonanceServiceInformation ServiceInformation { get; private set; }

        /// <summary>
        /// Gets the service credentials used for hub authentication.
        /// </summary>
        public TCredentials Credentials { get; private set; }

        /// <summary>
        /// Gets the SignalR mode (Legacy/Core).
        /// </summary>
        public SignalRMode Mode { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResonanceRegisteredService{TCredentials, TResonanceServiceInformation, TAdapterInformation}"/> class.
        /// </summary>
        /// <param name="credentials">The credentials.</param>
        /// <param name="serviceInformation">The service information.</param>
        /// <param name="mode">The mode.</param>
        /// <param name="signalRClient">The signal r client.</param>
        internal ResonanceRegisteredService(TCredentials credentials, TResonanceServiceInformation serviceInformation, SignalRMode mode, ISignalRClient signalRClient)
        {
            Mode = mode;
            Credentials = credentials;
            ServiceInformation = serviceInformation;
            _client = signalRClient;
            _client.On<String, TAdapterInformation>(ResonanceHubMethods.ConnectionRequest, OnConnectionRequest);
        }

        protected virtual SignalRAdapter<TCredentials> AcceptConnection(string sessionId)
        {
            return SignalRAdapter<TCredentials>.AcceptConnection(Credentials, _client.Url, ServiceInformation.ServiceId, sessionId, Mode);
        }

        protected virtual void OnConnectionRequest(string sessionId, TAdapterInformation adapterInformation)
        {
            ConnectionRequest?.Invoke(this, new ConnectionRequestEventArgs<TCredentials, TAdapterInformation>(AcceptConnection, DeclineConnection)
            {
                SessionId = sessionId,
                RemoteAdapterInformation = adapterInformation
            });
        }

        protected virtual void DeclineConnection(string sessionId)
        {
            _client.Invoke(ResonanceHubMethods.DeclineConnection, sessionId);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _client.Invoke(ResonanceHubMethods.UnregisterService).GetAwaiter().GetResult();
            _client?.Stop();
            _client?.Dispose();
            _client = null;
        }
    }
}
