using Microsoft.AspNetCore.SignalR;
using Resonance.Examples.SignalR.Common;
using Resonance.SignalR.Hubs;

namespace Resonance.Examples.SignalR.Server.Hubs
{
    public class DemoHub : ResonanceHubCore<DemoCredentials, DemoServiceInformation, DemoServiceInformation, DemoAdapterInformation, DemoHub>
    {
        public DemoHub(
            IHubContext<DemoHub> context,
            IResonanceHubProxy<DemoCredentials, DemoServiceInformation, DemoServiceInformation, DemoAdapterInformation> proxy)
            : base(context, proxy)
        {
        }
    }
}
