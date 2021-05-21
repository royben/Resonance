using Microsoft.Extensions.Logging;
using Resonance.Examples.Common.Logging;
using Resonance.Examples.WebRTC.Common;
using Resonance.SignalR.Hubs;
using Resonance.Transcoding.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Threading.Tasks;

namespace Resonance.Examples.WebRTC.Server.Hubs
{
    public class DemoHubProxy : ResonanceHubProxy<DemoCredentials, DemoServiceInformation, DemoServiceInformation, DemoAdapterInformation>
    {
        private static ConcurrentDictionary<String, LoggedInClient> _loggedInClients = new ConcurrentDictionary<string, LoggedInClient>();

        private ILogger Logger { get; set; }

        public DemoHubProxy(IResonanceHubRepository<DemoServiceInformation> repository) : base(repository)
        {
            Logger = ResonanceGlobalSettings.Default.LoggerFactory.CreateLogger("DemoHub");
        }

        protected override List<DemoServiceInformation> FilterServicesInformation(List<DemoServiceInformation> services, DemoCredentials credentials)
        {
            return services.ToList();
        }

        protected override DemoServiceInformation FilterServiceInformation(DemoServiceInformation service, DemoCredentials credentials)
        {
            return service;
        }

        protected override DemoAdapterInformation GetAdapterInformation(string connectionId)
        {
            return _loggedInClients[connectionId].AdapterInformation;
        }

        protected override void Login(DemoCredentials credentials, string connectionId, bool isDiscoveryClient)
        {
            if (!_loggedInClients.ContainsKey(connectionId))
            {
                _loggedInClients[connectionId] = new LoggedInClient()
                {
                    ConnectionId = connectionId,
                    Credentials = credentials,
                    AdapterInformation = new DemoAdapterInformation() { Name = credentials.Name },
                };

                if (!isDiscoveryClient)
                {
                    Logger.LogInformation($"{credentials.Name} is now logged in.");
                }
            }
        }

        public override string Connect(string serviceId)
        {
            Logger.LogInformation($"Connection Request: {_loggedInClients[GetConnectionId()].Credentials.Name} => {serviceId}");
            return base.Connect(serviceId);
        }

        public override void AcceptConnection(string sessionId)
        {
            base.AcceptConnection(sessionId);
            Logger.LogInformation($"Connection Accepted: {GetContextSession()?.Service.ServiceInformation.ServiceId} => {_loggedInClients[GetContextSession().ConnectedConnectionId].Credentials.Name}");
        }

        public override void RegisterService(DemoServiceInformation serviceInformation)
        {
            base.RegisterService(serviceInformation);
            Logger.LogInformation($"{serviceInformation.ServiceId} registered.");
        }

        public override void UnregisterService()
        {
            var service = Repository.GetService(x => x.ConnectionId == GetConnectionId());

            if (service != null)
            {
                Logger.LogInformation($"{service.ServiceInformation.ServiceId} unregistered.");
            }

            base.UnregisterService();
        }

        public override void Disconnect()
        {
            base.Disconnect();
            Logger.LogInformation($"{_loggedInClients[GetConnectionId()].Credentials.Name} disconnected.");
        }

        public override void Write(byte[] data)
        {
            base.Write(data);

            JsonDecoder decoder = new JsonDecoder();

            ResonanceDecodingInformation info = new ResonanceDecodingInformation();
            decoder.Decode(data, info);

            if (info.Type == ResonanceTranscodingInformationType.ContinuousRequest ||
                info.Type == ResonanceTranscodingInformationType.Message ||
                info.Type == ResonanceTranscodingInformationType.MessageSync ||
                info.Type == ResonanceTranscodingInformationType.Request ||
                info.Type == ResonanceTranscodingInformationType.Response)
            {
                Logger.LogInformation($"Write: {_loggedInClients[GetConnectionId()].Credentials.Name} => {_loggedInClients[GetOtherSideConnectionId()].Credentials.Name} => {{@Message}}", info.Message);
            }
        }

        protected override void Validate(string connectionId)
        {
            if (!_loggedInClients.ContainsKey(connectionId))
            {
                throw new AuthenticationException("The current client was not logged in.");
            }
        }

        protected override void OnConnectionClosed(String connectionId)
        {
            if (_loggedInClients.ContainsKey(connectionId))
            {
                Logger.LogInformation($"{_loggedInClients[connectionId].Credentials.Name} connection closed.");
            }
        }
    }
}
