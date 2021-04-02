using Microsoft.AspNet.SignalR.Client;
using Resonance.SignalR.Clients;
using Resonance.SignalR.Hubs;
using Resonance.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.SignalR.Services
{
    public class ResonanceServiceFactory
    {
        private static Lazy<ResonanceServiceFactory> _default = new Lazy<ResonanceServiceFactory>(() => new ResonanceServiceFactory());

        public static ResonanceServiceFactory Default
        {
            get { return _default.Value; }
        }

        private ResonanceServiceFactory()
        {

        }

        public async Task<List<TReportedServiceInformation>> GetAvailableServices<TCredentials, TReportedServiceInformation>(TCredentials credentials, String url, SignalRMode mode) where TReportedServiceInformation : IResonanceServiceInformation
        {
            ISignalRClient client = SignalRClientFactory.Default.Create(mode, url);

            await client.Start();
            await client.Invoke(ResonanceHubMethods.Login, credentials);
            var services = await client.Invoke<List<TReportedServiceInformation>>(ResonanceHubMethods.GetAvailableServices);
            await client.DisposeAsync();

            return services;
        }

        public async Task<ResonanceRegisteredService<TCredentials, TResonanceServiceInformation, TAdapterInformation>> RegisterService<TCredentials, TResonanceServiceInformation, TAdapterInformation>(TCredentials credentials, TResonanceServiceInformation serviceInformation, String url, SignalRMode mode) where TResonanceServiceInformation : IResonanceServiceInformation
        {
            ISignalRClient client = SignalRClientFactory.Default.Create(mode, url);

            await client.Start();
            await client.Invoke(ResonanceHubMethods.Login, credentials);
            await client.Invoke(ResonanceHubMethods.RegisterService, serviceInformation);
            return new ResonanceRegisteredService<TCredentials, TResonanceServiceInformation, TAdapterInformation>(credentials, serviceInformation, mode, client);
        }
    }
}
