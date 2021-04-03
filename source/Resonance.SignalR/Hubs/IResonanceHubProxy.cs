using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.SignalR.Hubs
{
    public delegate void InvokeClientMethodDelegate(String methodName, String connectionId, params object[] args);

    public interface IResonanceHubProxy<TCredentials, TServiceInformation, TReportedServiceInformation, TAdapterInformation> : 
        IResonanceHub<TCredentials, TServiceInformation, TReportedServiceInformation, TAdapterInformation>
        where TServiceInformation : IResonanceServiceInformation
        where TReportedServiceInformation : IResonanceServiceInformation
    {
        void Init(InvokeClientMethodDelegate invokeClient, Func<String> getConnectionId);
    }
}
