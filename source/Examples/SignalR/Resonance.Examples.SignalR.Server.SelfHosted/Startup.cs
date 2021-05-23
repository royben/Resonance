using Microsoft.AspNet.SignalR;
using Microsoft.Owin.Cors;
using Microsoft.Owin.FileSystems;
using Microsoft.Owin.StaticFiles;
using Owin;
using Resonance.Examples.Common.Logging;
using Resonance.Examples.SignalR.Common;
using Resonance.Examples.SignalR.Server.Hubs;
using Resonance.SignalR.Hubs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace Resonance.Examples.SignalR.Server
{
    public class Startup
    {
        // This code configures Web API. The Startup class is specified as a type
        // parameter in the WebApp.Start method.
        public void Configuration(IAppBuilder appBuilder)
        {
            // Configure Web API for self-host. 
            HttpConfiguration config = new HttpConfiguration();
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            appBuilder.UseWebApi(config);

            var fileSystem = new PhysicalFileSystem(".");
            var options = new FileServerOptions
            {
                EnableDefaultFiles = true,
                FileSystem = fileSystem
            };

            appBuilder.UseFileServer(options);

            //Configure the hub with dependency injection. (Use IOC container, not like this..)
            GlobalHost.DependencyResolver.Register(
                typeof(DemoHub),
                () => new DemoHub(new DemoHubProxy(new ResonanceHubMemoryRepository<DemoServiceInformation>())));

            GlobalHost.DependencyResolver.Register(typeof(LoggingHub), () => new LoggingHub());

            appBuilder.UseCors(CorsOptions.AllowAll);
            appBuilder.MapSignalR(new HubConfiguration()
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
