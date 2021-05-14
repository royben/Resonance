using Microsoft.AspNetCore.SignalR;
using Resonance.Examples.SignalR.Common;
using Resonance.SignalR.Hubs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Resonance.Examples.SignalR.WebApiService.Hubs.DemoHub
{
    public class DemoHub : ResonanceHubCore<DemoCredentials, DemoServiceInformation, DemoServiceInformation, DemoAdapterInformation, DemoHub>
    {
        public DemoHub(IHubContext<DemoHub> context, IResonanceHubProxy<DemoCredentials, DemoServiceInformation, DemoServiceInformation, DemoAdapterInformation> proxy) : base(context, proxy)
        {
        }
    }
}
