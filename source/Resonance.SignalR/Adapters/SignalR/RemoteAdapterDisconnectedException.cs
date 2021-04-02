using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.Adapters.SignalR
{
    public class RemoteAdapterDisconnectedException : Exception
    {
        public RemoteAdapterDisconnectedException() : base("The remote adapter has disconnected.")
        {

        }
    }
}
