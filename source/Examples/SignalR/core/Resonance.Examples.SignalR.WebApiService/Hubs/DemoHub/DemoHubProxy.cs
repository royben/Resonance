using Resonance.Examples.SignalR.Common;
using Resonance.SignalR.Hubs;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Threading.Tasks;

namespace Resonance.Examples.SignalR.WebApiService.Hubs.DemoHub
{
    public class DemoHubProxy : ResonanceHubProxy<DemoCredentials, DemoServiceInformation, DemoServiceInformation, DemoAdapterInformation>
    {
        private static ConcurrentDictionary<String, LoggedInClient> _loggedInClients = new ConcurrentDictionary<string, LoggedInClient>();

        public DemoHubProxy(IResonanceHubRepository<DemoServiceInformation> repository) : base(repository)
        {

        }

        protected override List<DemoServiceInformation> FilterServicesInformation(List<DemoServiceInformation> services, string connectionId)
        {
            return services.ToList();
        }

        protected override DemoAdapterInformation GetAdapterInformation(string connectionId)
        {
            return _loggedInClients[connectionId].AdapterInformation;
        }

        protected override void Login(DemoCredentials credentials, string connectionId)
        {
            _loggedInClients[connectionId] = new LoggedInClient()
            {
                ConnectionId = connectionId,
                Credentials = credentials,
                AdapterInformation = new DemoAdapterInformation() { Name = credentials.Name },
            };
        }

        protected override void Validate(string connectionId)
        {
            if (!_loggedInClients.ContainsKey(connectionId))
            {
                throw new AuthenticationException("The current client was not logged in.");
            }
        }
    }
}
