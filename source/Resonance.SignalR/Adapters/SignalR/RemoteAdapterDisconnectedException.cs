using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.Adapters.SignalR
{
    /// <summary>
    /// Represents an exception that is thrown when the remote SignalR adapter has disconnected.
    /// </summary>
    /// <seealso cref="System.Exception" />
    public class RemoteAdapterDisconnectedException : Exception
    {
        public RemoteAdapterDisconnectedException() : base("The remote adapter has disconnected.")
        {

        }
    }
}
