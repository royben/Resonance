using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance.RPC
{
    /// <summary>
    /// Represents a remote procedure call member type.
    /// </summary>
    public enum RPCSignatureType
    {
        /// <summary>
        /// Method call.
        /// </summary>
        Method,
        /// <summary>
        /// Property getter/setter.
        /// </summary>
        Property,
        /// <summary>
        /// Event registration.
        /// </summary>
        Event
    }
}
