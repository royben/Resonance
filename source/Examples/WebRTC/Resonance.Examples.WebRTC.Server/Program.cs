using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Resonance.Examples.Common.Logging;
using Resonance.Examples.WebRTC.Common;
using Resonance.Examples.WebRTC.Server.Hubs;
using Resonance.SignalR.Hubs;
using System;

namespace Resonance.Examples.WebRTC.Server
{
    public class Program
    {
        private const String Address = "http://localhost:8081";

        public static void Main(String[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.WebHost.UseUrls(Address);

            builder.Services.AddTransient<
                IResonanceHubRepository<DemoServiceInformation>,
                ResonanceHubMemoryRepository<DemoServiceInformation>>();

            builder.Services.AddTransient<
                IResonanceHubProxy<DemoCredentials, DemoServiceInformation, DemoServiceInformation, DemoAdapterInformation>,
                DemoHubProxy>();

            builder.Services
                .AddSignalR(options =>
                {
                    options.EnableDetailedErrors = true;
                    options.MaximumReceiveMessageSize = null; //Unlimited message size.
                });

            var app = builder.Build();

            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.MapHub<DemoHub>("/hubs/DemoHub");
            app.MapHub<LoggingHub>("/hubs/LoggingHub");

            LoggingConfiguration.ConfigureLogging();
            LoggingHub.Attach(app.Services.GetRequiredService<IHubContext<LoggingHub>>());

            Console.WriteLine($"Resonance WebRTC Demo Server started on {Address}");

            app.Run();
        }
    }
}
