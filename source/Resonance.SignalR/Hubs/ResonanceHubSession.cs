using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.SignalR.Hubs
{
    public class ResonanceHubSession<TServiceInformation> where TServiceInformation : IResonanceServiceInformation
    {
        public String SessionId { get; }

        public ResonanceHubRegisteredService<TServiceInformation> Service { get; set; }

        public String ConnectedConnectionId { get; set; }

        public String AcceptedConnectionId { get; set; }

        public ResonanceHubSession()
        {
            SessionId = Guid.NewGuid().ToString();
        }
    }
}
