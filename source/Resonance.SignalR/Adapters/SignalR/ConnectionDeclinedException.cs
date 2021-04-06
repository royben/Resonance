using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.Adapters.SignalR
{
    /// <summary>
    /// Represents an exception that is thrown when the remote SignalR service has declined an incoming connection.
    /// </summary>
    /// <seealso cref="System.Exception" />
    public class ConnectionDeclinedException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionDeclinedException"/> class.
        /// </summary>
        public ConnectionDeclinedException() : base("The remote service has declined the connection request.")
        {

        }
    }
}
