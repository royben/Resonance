using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance.RPC
{
    /// <summary>
    /// Represents an RPC service registration creation type.
    /// </summary>
    public enum RpcServiceCreationType
    {
        /// <summary>
        /// The service will be created only once on the first request.
        /// </summary>
        Singleton,

        /// <summary>
        /// The service will be created on each incoming message/request.
        /// </summary>
        Transient,
    }
}
