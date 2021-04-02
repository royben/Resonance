using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.SignalR.Hubs
{
    public abstract class ResonanceHubCore<TInterface, TCredentials, TServiceInformation, TReportedServiceInformation, TAdapterInformation>
        : Hub<TInterface>,
        IResonanceHub<TCredentials, TServiceInformation, TReportedServiceInformation, TAdapterInformation>
        where TInterface : class
        where TServiceInformation : IResonanceServiceInformation
        where TReportedServiceInformation : IResonanceServiceInformation
    {
        public void AcceptConnection(string sessionId)
        {
            throw new NotImplementedException();
        }

        public string Connect(string serviceId)
        {
            throw new NotImplementedException();
        }

        public void DeclineConnection(string sessionId)
        {
            throw new NotImplementedException();
        }

        public void Disconnect()
        {
            throw new NotImplementedException();
        }

        public List<TReportedServiceInformation> GetAvailableServices()
        {
            throw new NotImplementedException();
        }

        public void Login(TCredentials credentials)
        {
            throw new NotImplementedException();
        }

        public void RegisterService(TServiceInformation serviceInformation)
        {
            throw new NotImplementedException();
        }

        public void UnregisterService()
        {
            throw new NotImplementedException();
        }

        public void Write(byte[] data)
        {
            throw new NotImplementedException();
        }
    }
}
