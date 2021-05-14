using Resonance.Examples.SignalR.Common;
using Resonance.SignalR.Hubs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Resonance.Examples.SignalR.Server.Hubs.DemoHub
{
    public class DemoHub : ResonanceHub<DemoCredentials, DemoServiceInformation, DemoServiceInformation, DemoAdapterInformation, DemoHub>
    {
        public DemoHub(IResonanceHubProxy<DemoCredentials, DemoServiceInformation, DemoServiceInformation, DemoAdapterInformation> proxy) : base(proxy)
        {

        }
    }
}
