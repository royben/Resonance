using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.SignalR.Hubs
{
    public abstract class ResonanceHubCore<TCredentials, TServiceInformation, TReportedServiceInformation, TAdapterInformation, THub, THubProxy>
           : Hub,
           IResonanceHub<TCredentials, TServiceInformation, TReportedServiceInformation, TAdapterInformation>
           where TServiceInformation : IResonanceServiceInformation
           where TReportedServiceInformation : IResonanceServiceInformation
           where THub : ResonanceHubCore<TCredentials, TServiceInformation, TReportedServiceInformation, TAdapterInformation, THub, THubProxy>
           where THubProxy : ResonanceHubProxy<TCredentials, TServiceInformation, TReportedServiceInformation, TAdapterInformation>, new()
    {
        private THubProxy _proxy;
        private IHubContext<THub> _context;

        public ResonanceHubCore(IHubContext<THub> context)
        {
            _context = context;
            _proxy = new THubProxy();
            _proxy.Init(InvokeClient, GetConnectionId);
        }

        public void Login(TCredentials credentials)
        {
            _proxy.Login(credentials);
        }

        public void RegisterService(TServiceInformation serviceInformation)
        {
            _proxy.RegisterService(serviceInformation);
        }

        public void UnregisterService()
        {
            _proxy.UnregisterService();
        }

        public List<TReportedServiceInformation> GetAvailableServices()
        {
            return _proxy.GetAvailableServices();
        }

        public string Connect(string serviceId)
        {
            return _proxy.Connect(serviceId);
        }

        public void AcceptConnection(string sessionId)
        {
            _proxy.AcceptConnection(sessionId);
        }

        public void DeclineConnection(string sessionId)
        {
            _proxy.DeclineConnection(sessionId);
        }

        public void Disconnect()
        {
            _proxy.Disconnect();
        }

        public void Write(byte[] data)
        {
            _proxy.Write(data);
        }

        private string GetConnectionId()
        {
            return Context.ConnectionId;
        }

        private async void InvokeClient(string methodName, string connectionId, object[] args)
        {
            IClientProxy proxy = _context.Clients.Client(connectionId);
            await proxy.SendCoreAsync(methodName, args);
        }
    }
}
