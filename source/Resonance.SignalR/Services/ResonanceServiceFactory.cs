using Microsoft.AspNet.SignalR.Client;
using Microsoft.Extensions.Logging;
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
    /// <summary>
    /// Represents a Resonance SignalR service factory used for querying and registering services.
    /// </summary>
    public class ResonanceServiceFactory : ResonanceObject
    {
        private static Lazy<ResonanceServiceFactory> _default = new Lazy<ResonanceServiceFactory>(() => new ResonanceServiceFactory());

        /// <summary>
        /// Gets the default instance.
        /// </summary>
        public static ResonanceServiceFactory Default
        {
            get { return _default.Value; }
        }

        /// <summary>
        /// Prevents a default instance of the <see cref="ResonanceServiceFactory"/> class from being created.
        /// </summary>
        private ResonanceServiceFactory()
        {

        }

        /// <summary>
        /// Gets the collection of available services for the provided credentials.
        /// </summary>
        /// <typeparam name="TCredentials">The type of the credentials.</typeparam>
        /// <typeparam name="TReportedServiceInformation">The type of the reported service information.</typeparam>
        /// <param name="credentials">The credentials.</param>
        /// <param name="url">The URL.</param>
        /// <param name="mode">The mode.</param>
        /// <returns></returns>
        public async Task<List<TReportedServiceInformation>> GetAvailableServicesAsync<TCredentials, TReportedServiceInformation>(TCredentials credentials, String url, SignalRMode mode) where TReportedServiceInformation : IResonanceServiceInformation
        {
            ISignalRClient client = SignalRClientFactory.Default.Create(mode, url);

            await client.Start();
            await client.Invoke(ResonanceHubMethods.Login, credentials);
            var services = await client.Invoke<List<TReportedServiceInformation>>(ResonanceHubMethods.GetAvailableServices);
            await client.DisposeAsync();

            return services;
        }

        /// <summary>
        /// Gets the collection of available services for the provided credentials.
        /// </summary>
        /// <typeparam name="TCredentials">The type of the credentials.</typeparam>
        /// <typeparam name="TReportedServiceInformation">The type of the reported service information.</typeparam>
        /// <param name="credentials">The credentials.</param>
        /// <param name="url">The URL.</param>
        /// <param name="mode">The mode.</param>
        /// <returns></returns>
        public List<TReportedServiceInformation> GetAvailableServices<TCredentials, TReportedServiceInformation>(TCredentials credentials, String url, SignalRMode mode) where TReportedServiceInformation : IResonanceServiceInformation
        {
            return GetAvailableServicesAsync<TCredentials, TReportedServiceInformation>(credentials, url, mode).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Registers a new Resonance SignalR service.
        /// </summary>
        /// <typeparam name="TCredentials">The type of the credentials.</typeparam>
        /// <typeparam name="TResonanceServiceInformation">The type of the resonance service information.</typeparam>
        /// <typeparam name="TAdapterInformation">The type of the adapter information.</typeparam>
        /// <param name="credentials">The credentials used to authenticate the service.</param>
        /// <param name="serviceInformation">The service information.</param>
        /// <param name="url">The hub URL.</param>
        /// <param name="mode">The SignalR mode (legacy/core).</param>
        /// <returns></returns>
        public async Task<ResonanceRegisteredService<TCredentials, TResonanceServiceInformation, TAdapterInformation>> RegisterServiceAsync<TCredentials, TResonanceServiceInformation, TAdapterInformation>(TCredentials credentials, TResonanceServiceInformation serviceInformation, String url, SignalRMode mode) where TResonanceServiceInformation : IResonanceServiceInformation
        {
            Logger.LogDebug($"Registering service {{@ServiceInformation}}...", serviceInformation);

            ISignalRClient client = SignalRClientFactory.Default.Create(mode, url);

            await client.Start();
            await client.Invoke(ResonanceHubMethods.Login, credentials);
            await client.Invoke(ResonanceHubMethods.RegisterService, serviceInformation);
            return new ResonanceRegisteredService<TCredentials, TResonanceServiceInformation, TAdapterInformation>(credentials, serviceInformation, mode, client);
        }

        /// <summary>
        /// Registers a new Resonance SignalR service.
        /// </summary>
        /// <typeparam name="TCredentials">The type of the credentials.</typeparam>
        /// <typeparam name="TResonanceServiceInformation">The type of the resonance service information.</typeparam>
        /// <typeparam name="TAdapterInformation">The type of the adapter information.</typeparam>
        /// <param name="credentials">The credentials used to authenticate the service.</param>
        /// <param name="serviceInformation">The service information.</param>
        /// <param name="url">The hub URL.</param>
        /// <param name="mode">The SignalR mode (legacy/core).</param>
        /// <returns></returns>
        public ResonanceRegisteredService<TCredentials, TResonanceServiceInformation, TAdapterInformation> RegisterService<TCredentials, TResonanceServiceInformation, TAdapterInformation>(TCredentials credentials, TResonanceServiceInformation serviceInformation, String url, SignalRMode mode) where TResonanceServiceInformation : IResonanceServiceInformation
        {
            return RegisterServiceAsync<TCredentials, TResonanceServiceInformation, TAdapterInformation>(credentials, serviceInformation, url, mode).GetAwaiter().GetResult();
        }
    }
}
