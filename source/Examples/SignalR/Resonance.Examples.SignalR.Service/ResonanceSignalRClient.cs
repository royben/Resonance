using Resonance.Examples.SignalR.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.Examples.SignalR.Service
{
    public class ResonanceSignalRClient : ResonanceTransporter
    {
        public DemoAdapterInformation RemoteAdapterInformation { get; set; }
    }
}
