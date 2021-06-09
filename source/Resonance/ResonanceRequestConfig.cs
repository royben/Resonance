﻿using Resonance.RPC;
using Resonance.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Resonance
{
    /// <summary>
    /// Represents an <see cref="ResonanceMessage"/> configuration.
    /// </summary>
    public class ResonanceRequestConfig
    {
        /// <summary>
        /// Gets or sets the request timeout.
        /// </summary>
        public TimeSpan? Timeout { get; set; }

        /// <summary>
        /// Gets or sets the request logging mode.
        /// </summary>
        public ResonanceMessageLoggingMode LoggingMode { get; set; }

        /// <summary>
        /// Gets or sets the request queue priority.
        /// </summary>
        public QueuePriority Priority { get; set; }

        /// <summary>
        /// Gets or sets the request cancellation token.
        /// </summary>
        public CancellationToken? CancellationToken { get; set; }

        /// <summary>
        /// Gets or sets the RPC signature.
        /// </summary>
        internal RPCSignature RPCSignature { get; set; }
    }
}
