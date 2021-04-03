using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.SignalR.Hubs
{
    public abstract class ResonanceHub<TCredentials, TServiceInformation, TReportedServiceInformation, TAdapterInformation, THub>
        : Hub,
        IResonanceHub<TCredentials, TServiceInformation, TReportedServiceInformation, TAdapterInformation>
        where TServiceInformation : IResonanceServiceInformation
        where TReportedServiceInformation : IResonanceServiceInformation
        where THub : ResonanceHub<TCredentials, TServiceInformation, TReportedServiceInformation, TAdapterInformation, THub>
    {
        private IResonanceHubProxy<TCredentials, TServiceInformation, TReportedServiceInformation, TAdapterInformation> _proxy;

        public ResonanceHub(IResonanceHubProxy<TCredentials, TServiceInformation, TReportedServiceInformation, TAdapterInformation> proxy)
        {
            _proxy = proxy;
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

        private void InvokeClient(string methodName, string connectionId, object[] args)
        {
            var ctx = GlobalHost.ConnectionManager.GetHubContext<THub>();
            IClientProxy proxy = ctx.Clients.Client(connectionId);
            proxy.Invoke(methodName, args);
        }
    }
}
