using Resonance.SignalR.Hubs;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.Tests.SignalR
{
    public class TestHubProxy : ResonanceHubProxy<TestCredentials, TestServiceInformation, TestServiceInformation, TestAdapterInformation>
    {
        private static ConcurrentDictionary<String, TestCredentials> _loggedInClients = new ConcurrentDictionary<string, TestCredentials>();

        public TestHubProxy(IResonanceHubRepository<TestServiceInformation> repository) : base(repository)
        {

        }

        protected override void Login(TestCredentials credentials, string connectionId)
        {
            if (credentials.Name != "Test")
            {
                throw new AuthenticationException("Name is not 'Test'.");
            }

            _loggedInClients[connectionId] = credentials;
        }

        protected override void Validate(string connectionId)
        {
            if (!_loggedInClients.ContainsKey(connectionId))
            {
                throw new AuthenticationException("The current client was not logged in.");
            }
        }

        protected override List<TestServiceInformation> FilterServicesInformation(List<TestServiceInformation> services, TestCredentials credentials)
        {
            return services.ToList();
        }

        protected override TestAdapterInformation GetAdapterInformation(string connectionId)
        {
            return new TestAdapterInformation() { Information = "No information on the remote adapter" };
        }
    }
}
