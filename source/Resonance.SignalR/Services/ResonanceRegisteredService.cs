using Microsoft.AspNet.SignalR.Client;
using Resonance.Adapters.SignalR;
using Resonance.SignalR.Hubs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.SignalR.Services
{
    public class ResonanceRegisteredService<TCredentials, TResonanceServiceInformation, TAdapterInformation> : ResonanceRegisteredServiceBase<TCredentials, TResonanceServiceInformation, TAdapterInformation> where TResonanceServiceInformation : IResonanceServiceInformation
    {
        private HubConnection _connection;
        private IHubProxy _proxy;

        public String Hub { get; private set; }

        public ResonanceRegisteredService(TCredentials credentials, TResonanceServiceInformation serviceInformation, String url, String hub, HubConnection hubConnection, IHubProxy proxy) : base(credentials, serviceInformation, url)
        {
            _connection = hubConnection;
            _proxy = proxy;
            Hub = hub;
            _proxy.On<String, TAdapterInformation>(ResonanceHubMethods.ConnectionRequest, OnConnectionRequest);
        }

        protected override Task<ISignalRAdapter<TCredentials>> AcceptConnection(string sessionId)
        {
            return Task.FromResult((ISignalRAdapter<TCredentials>)SignalRAdapter<TCredentials>.AcceptConnection(Url, Hub, ServiceInformation.ServiceId, sessionId, Credentials));
        }

        protected override Task DeclineConnection(string sessionId)
        {
            return _proxy.Invoke(ResonanceHubMethods.DeclineConnection, sessionId);
        }

        public override void Dispose()
        {
            _proxy.Invoke(ResonanceHubMethods.UnregisterService).GetAwaiter().GetResult();
            _connection?.Stop();
            _connection?.Dispose();
            _connection = null;
        }
    }
}
