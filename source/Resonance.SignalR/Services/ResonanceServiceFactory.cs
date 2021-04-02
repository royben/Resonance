using Microsoft.AspNet.SignalR.Client;
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

        public async Task<List<TReportedServiceInformation>> GetAvailableServices<TCredentials, TReportedServiceInformation>(TCredentials credentials, String url, String hub) where TReportedServiceInformation : IResonanceServiceInformation
        {
            var connection = new HubConnection(url);
            var proxy = connection.CreateHubProxy(hub);
            await connection.Start();
            await proxy.Invoke(ResonanceHubMethods.Login, credentials);
            var services = await proxy.Invoke<List<TReportedServiceInformation>>(ResonanceHubMethods.GetAvailableServices);

            await Task.Factory.StartNew(() =>
            {
                connection.Stop();
                connection.Dispose();
            });

            return services;
        }

        public Task<ResonanceRegisteredService<TCredentials, TResonanceServiceInformation, TAdapterInformation>> RegisterService<TCredentials, TResonanceServiceInformation, TAdapterInformation>(TCredentials credentials, TResonanceServiceInformation serviceInformation, String url, String hub) where TResonanceServiceInformation : IResonanceServiceInformation
        {
            TaskCompletionSource<ResonanceRegisteredService<TCredentials, TResonanceServiceInformation, TAdapterInformation>> completionSource = new TaskCompletionSource<ResonanceRegisteredService<TCredentials, TResonanceServiceInformation, TAdapterInformation>>();

            bool completed = false;

            Task.Factory.StartNew(() =>
            {
                var connection = new HubConnection(url);
                var proxy = connection.CreateHubProxy(hub);
                connection.StateChanged += (x) =>
                {
                    if (x.NewState == ConnectionState.Connected)
                    {
                        if (completed) return;
                        completed = true;

                        Task.Factory.StartNew(() =>
                        {
                            try
                            {
                                proxy.Invoke(ResonanceHubMethods.Login, credentials).GetAwaiter().GetResult();
                                proxy.Invoke(ResonanceHubMethods.RegisterService, serviceInformation).GetAwaiter().GetResult();
                                completionSource.SetResult(new ResonanceRegisteredService<TCredentials, TResonanceServiceInformation, TAdapterInformation>(credentials, serviceInformation, url, hub, connection, proxy));
                            }
                            catch (Exception ex)
                            {
                                completionSource.SetException(ex);
                            }
                        });
                    }
                };

                connection.Start().GetAwaiter().GetResult();
            });

            TimeoutTask.StartNew(() =>
            {
                if (!completed)
                {
                    completed = true;
                    completionSource.SetException(new TimeoutException("Could not complete the request within the given timeout."));
                }

            }, TimeSpan.FromSeconds(10));


            return completionSource.Task;
        }
    }
}
