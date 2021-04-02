using Resonance.SignalR.Hubs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.Tests.SignalR
{
    public class TestHubProxy : ResonanceHubMemoryProxy<TestCredentials, TestServiceInformation, TestServiceInformation, TestAdapterInformation>
    {
        protected override List<TestServiceInformation> FilterServicesInformation(List<TestServiceInformation> services)
        {
            return services.ToList();
        }

        protected override TestAdapterInformation GetAdapterInformation(string connectionId)
        {
            return new TestAdapterInformation() { Information = "No information on the remote adapter" };
        }

        protected override void OnLogin(TestCredentials credentials)
        {
            if (credentials.Name != "Test")
            {
                throw new AuthenticationException("Name is not 'Test'.");
            }
        }
    }
}
