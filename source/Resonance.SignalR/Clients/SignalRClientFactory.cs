using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.SignalR.Clients
{
    internal class SignalRClientFactory
    {
        private static Lazy<SignalRClientFactory> _default = new Lazy<SignalRClientFactory>(() => new SignalRClientFactory());

        public static SignalRClientFactory Default
        {
            get { return _default.Value; }
        }

        private SignalRClientFactory()
        {

        }

        public ISignalRClient Create(SignalRMode mode, String url)
        {
#if NET461
            if (mode == SignalRMode.Legacy) return new SignalRClient(url);
            return new SignalRCoreClient(url);
#else
            return new SignalRCoreClient(url);
#endif
        }
    }
}
