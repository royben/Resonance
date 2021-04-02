using Resonance.SignalR.Hubs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.Tests.SignalR
{
    public class TestHub : ResonanceHub<TestCredentials, TestServiceInformation, TestServiceInformation, TestAdapterInformation>
    {
        private static Dictionary<String, TestCredentials> _loggedInClients = new Dictionary<string, TestCredentials>();

        protected override void Login(TestCredentials credentials, string connectionId)
        {
            if (credentials.Name != "Test")
            {
                throw new AuthenticationException("Name is not 'Test'.");
            }

            _loggedInClients.Add(connectionId, credentials);
        }

        protected override void Validate(string connectionId)
        {
            if (!_loggedInClients.ContainsKey(connectionId))
            {
                throw new AuthenticationException("The current client was not logged in.");
            }
        }

        protected override TestAdapterInformation GetAdapterInformation(string connectionId)
        {
            return new TestAdapterInformation();
        }

        protected override List<TestServiceInformation> FilterServicesInformation(List<TestServiceInformation> services)
        {
            return services.ToList();
        }
    }
}
