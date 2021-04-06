using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.Adapters.SignalR
{
    /// <summary>
    /// Represents a <see cref="SignalRAdapter{TCredentials}"/> role.
    /// </summary>
    public enum SignalRAdapterRole
    {
        /// <summary>
        /// The adapter is the initiator of the connection.
        /// </summary>
        Connect,

        /// <summary>
        /// The adapter should accept a connection.
        /// </summary>
        Accept
    }
}
