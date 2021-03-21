using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance
{
    public class ResonanceRequestReceivedEventArgs
    {
        public ResonanceRequest Request { get; set; }
        public bool Handled { get; set; }

        public ResonanceRequestReceivedEventArgs()
        {

        }

        public ResonanceRequestReceivedEventArgs(ResonanceRequest request) : this()
        {
            Request = request;
        }
    }
}
