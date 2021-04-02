using Microsoft.AspNet.SignalR.Client;
using Resonance.Adapters.SignalR;
using Resonance.SignalR.Hubs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.SignalR.Services
{
    public abstract class ResonanceRegisteredServiceBase<TCredentials, TResonanceServiceInformation, TAdapterInformation> : IDisposable where TResonanceServiceInformation : IResonanceServiceInformation
    {
        public event EventHandler<ConnectionRequestEventArgs<TCredentials, TAdapterInformation>> ConnectionRequest;

        public TResonanceServiceInformation ServiceInformation { get; private set; }

        public TCredentials Credentials { get; private set; }

        public String Url { get; private set; }

        public ResonanceRegisteredServiceBase(TCredentials credentials, TResonanceServiceInformation serviceInformation, String url)
        {
            Credentials = credentials;
            ServiceInformation = serviceInformation;
            Url = url;
        }

        protected virtual void OnConnectionRequest(string sessionId, TAdapterInformation adapterInformation)
        {
            ConnectionRequest?.Invoke(this, new ConnectionRequestEventArgs<TCredentials, TAdapterInformation>(AcceptConnection, DeclineConnection)
            {
                SessionId = sessionId,
                RemoteAdapterInformation = adapterInformation
            });
        }

        protected abstract Task<ISignalRAdapter<TCredentials>> AcceptConnection(String sessionId);
        protected abstract Task DeclineConnection(String sessionId);

        public abstract void Dispose();
    }
}
