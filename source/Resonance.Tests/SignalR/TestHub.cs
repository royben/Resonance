using Resonance.SignalR.Hubs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.Tests.SignalR
{
    public class TestHub :
        ResonanceHub<TestCredentials,
            TestServiceInformation,
            TestServiceInformation,
            TestAdapterInformation,
            TestHub>
    {
        public TestHub(IResonanceHubProxy<TestCredentials, TestServiceInformation, TestServiceInformation, TestAdapterInformation> proxy) : base(proxy)
        {
        }
    }
}
