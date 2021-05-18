using Microsoft.AspNet.SignalR;
using Microsoft.Owin;
using Microsoft.Owin.Cors;
using Owin;
using Resonance.Examples.Common.Logging;
using Resonance.Examples.SignalR.Common;
using Resonance.Examples.SignalR.Server;
using Resonance.Examples.SignalR.Server.Hubs;
using Resonance.SignalR.Hubs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

[assembly: OwinStartup(typeof(Startup))]
namespace Resonance.Examples.SignalR.Server
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            //Configure the hub with dependency injection. (Use IOC container, not like this..)
            GlobalHost.DependencyResolver.Register(
                typeof(DemoHub),
                () => new DemoHub(new DemoHubProxy(new ResonanceHubMemoryRepository<DemoServiceInformation>())));

            GlobalHost.DependencyResolver.Register(typeof(LoggingHub), () => new LoggingHub());

            app.UseCors(CorsOptions.AllowAll);
            app.MapSignalR(new HubConfiguration() 
            {
                EnableDetailedErrors = true //Include exception messages from hub!
            });

            GlobalHost.Configuration.MaxIncomingWebSocketMessageSize = null; //Unlimited message size.

            LoggingConfiguration.ConfigureLogging();

            LoggingConfiguration.LogReceived += (x, e) => 
            {
                LoggingHub.PublishLog(e);
            };
        }
    }
}