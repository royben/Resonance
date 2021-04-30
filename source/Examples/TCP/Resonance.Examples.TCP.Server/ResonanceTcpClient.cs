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

        protected async override void OnRequestReceived(ResonanceRequest request)
        {
            if (InSession)
            {
                await RemoteClient?.SendRequest(request);
            }
        }

        protected async override void OnResponseReceived(ResonanceResponse response)
        {
            if (InSession)
            {
                await RemoteClient?.SendResponse(response);
            }
        }
    }
}
