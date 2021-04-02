using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.Adapters.SignalR
{
    public class ConnectionDeclinedException : Exception
    {
        public ConnectionDeclinedException() : base("The remote service has declined the connection request.")
        {

        }
    }
}
