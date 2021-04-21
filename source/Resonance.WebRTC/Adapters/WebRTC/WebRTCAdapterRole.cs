using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance.Adapters.WebRTC
{
    /// <summary>
    /// Represents a <see cref="WebRTCAdapter"/> role.
    /// </summary>
    public enum WebRTCAdapterRole
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
