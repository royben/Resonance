using Resonance.RPC;
using Resonance.Threading;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Resonance
{
    /// <summary>
    /// Represents a standard message configuration.
    /// </summary>
    public class ResonanceMessageConfig
    {
        /// <summary>
        /// Gets or sets the message timeout.
        /// Valid only when <see cref="RequireACK"/> is set to true.
        /// </summary>
        public TimeSpan? Timeout { get; set; }

        /// <summary>
        /// Gets or sets the message logging mode.
        /// </summary>
        public ResonanceMessageLoggingMode LoggingMode { get; set; }

        /// <summary>
        /// Gets or sets the message queue priority.
        /// </summary>
        public QueuePriority Priority { get; set; }

        /// <summary>
        /// Gets or sets the request cancellation token.
        /// </summary>
        public CancellationToken? CancellationToken { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to require reception confirmation from the remote peer.
        /// </summary>
        public bool RequireACK { get; set; }

        /// <summary>
        /// Gets or sets the RPC signature.
        /// </summary>
        internal RPCSignature RPCSignature { get; set; }
    }
}
