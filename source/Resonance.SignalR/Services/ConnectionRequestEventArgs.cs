using Resonance.Adapters.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.SignalR.Services
{
    public class ConnectionRequestEventArgs<TCredentials, TAdapterInformation> : EventArgs
    {
        private Func<String, Task<ISignalRAdapter<TCredentials>>> _accept;
        private Func<String, Task> _decline;

        public String SessionId { get; set; }

        public TAdapterInformation RemoteAdapterInformation { get; set; }

        public ConnectionRequestEventArgs(Func<String, Task<ISignalRAdapter<TCredentials>>> accept, Func<String, Task> decline)
        {
            _accept = accept;
            _decline = decline;
        }

        public Task<ISignalRAdapter<TCredentials>> Accept()
        {
            return _accept?.Invoke(SessionId);
        }

        public Task Decline()
        {
            return _decline?.Invoke(SessionId);
        }
    }
}
