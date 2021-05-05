using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.Examples.TCP.Server
{
    public class ResonanceTcpClient : ResonanceTransporter
    {
        public String ClientID { get; set; }

        public bool InSession { get; set; }

        public ResonanceTcpClient RemoteClient { get; set; }

        protected async override void OnRequestReceived(ResonanceMessage request)
        {
            if (InSession)
            {
                await RemoteClient?.SendRequestAsync(request);
            }
        }

        protected async override void OnResponseReceived(ResonanceMessage response)
        {
            if (InSession)
            {
                await RemoteClient?.SendResponseAsync(response);
            }
        }

        protected async override void OnMessageReceived(ResonanceMessage message)
        {
            if (InSession)
            {
                await RemoteClient?.SendAsync(message);
            }
        }
    }
}
