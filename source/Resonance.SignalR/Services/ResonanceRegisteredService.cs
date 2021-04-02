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
    public class ResonanceRegisteredService<TCredentials, TResonanceServiceInformation, TAdapterInformation> : IDisposable where TResonanceServiceInformation : IResonanceServiceInformation
    {
        private ISignalRClient _client;

        public event EventHandler<ConnectionRequestEventArgs<TCredentials, TAdapterInformation>> ConnectionRequest;

        public TResonanceServiceInformation ServiceInformation { get; private set; }

        public TCredentials Credentials { get; private set; }

        public SignalRMode Mode { get; private set; }

        internal ResonanceRegisteredService(TCredentials credentials, TResonanceServiceInformation serviceInformation, SignalRMode mode, ISignalRClient signalRClient)
        {
            Mode = mode;
            Credentials = credentials;
            ServiceInformation = serviceInformation;
            _client = signalRClient;
            _client.On<String, TAdapterInformation>(ResonanceHubMethods.ConnectionRequest, OnConnectionRequest);
        }

        protected Task<ISignalRAdapter<TCredentials>> AcceptConnection(string sessionId)
        {
            return Task.FromResult((ISignalRAdapter<TCredentials>)SignalRAdapter<TCredentials>.AcceptConnection(Credentials, _client.Url, ServiceInformation.ServiceId, sessionId, Mode));
        }

        protected virtual void OnConnectionRequest(string sessionId, TAdapterInformation adapterInformation)
        {
            ConnectionRequest?.Invoke(this, new ConnectionRequestEventArgs<TCredentials, TAdapterInformation>(AcceptConnection, DeclineConnection)
            {
                SessionId = sessionId,
                RemoteAdapterInformation = adapterInformation
            });
        }

        protected Task DeclineConnection(string sessionId)
        {
            return _client.Invoke(ResonanceHubMethods.DeclineConnection, sessionId);
        }

        public void Dispose()
        {
            _client.Invoke(ResonanceHubMethods.UnregisterService).GetAwaiter().GetResult();
            _client?.Stop();
            _client?.Dispose();
            _client = null;
        }
    }
}
