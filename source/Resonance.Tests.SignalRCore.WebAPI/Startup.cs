using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Resonance.SignalR.Hubs;
using Resonance.Tests.SignalRCore.WebAPI.Hub;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Resonance.Tests.SignalRCore.WebAPI
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<IResonanceHubRepository<TestServiceInformation>, ResonanceHubMemoryRepository<TestServiceInformation>>();
            services.AddTransient<IResonanceHubProxy<TestCredentials, TestServiceInformation, TestServiceInformation, TestAdapterInformation>, TestHubProxy>();
            services.AddControllers();


            services
                .AddSignalR((x) => x.EnableDetailedErrors = true)
                .AddMessagePackProtocol(); //Add MessagePack protocol !
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHub<TestHub>("/hubs/TestHub");
            });
        }
    }
}
