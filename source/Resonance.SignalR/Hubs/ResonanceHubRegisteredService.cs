using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.SignalR.Hubs
{
    public class ResonanceHubRegisteredService<TServiceInformation> where TServiceInformation : IResonanceServiceInformation
    {
        public String ConnectionId { get; set; }
        public TServiceInformation ServiceInformation { get; set; }
    }
}
