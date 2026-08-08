using Microsoft.AspNetCore.SignalR;
using Resonance.Examples.WebRTC.Common;
using Resonance.SignalR.Hubs;

namespace Resonance.Examples.WebRTC.Server.Hubs
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
