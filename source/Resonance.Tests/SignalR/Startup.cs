using Microsoft.AspNet.SignalR;
using Microsoft.Owin.Cors;
using Microsoft.Owin.Hosting;
using Owin;
using Resonance.SignalR;
using Resonance.SignalR.Hubs;
using Resonance.Tests.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.Tests //This must be here because OWIN requires AssemblyName.Startup class.
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            //Configure the hub with dependency injection. (Use IOC container, not like this..)
            GlobalHost.DependencyResolver.Register(
                typeof(TestHub), 
                () => new TestHub(new TestHubProxy(new ResonanceHubMemoryRepository<TestServiceInformation>())));

            app.UseCors(CorsOptions.AllowAll);
            app.MapSignalR(new HubConfiguration() { EnableDetailedErrors = true }); //Use this to include exception messages from hub.
        }
    }
}
