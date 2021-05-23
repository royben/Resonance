using Resonance.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.Examples.WebRTC.Common
{
    public class DemoServiceInformation : IResonanceServiceInformation
    {
        public string ServiceId { get; set; }
    }
}
