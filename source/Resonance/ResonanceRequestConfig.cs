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
    /// Represents an <see cref="ResonanceRequest"/> configuration.
    /// </summary>
    public class ResonanceRequestConfig
    {
        /// <summary>
        /// Gets or sets the request timeout.
        /// </summary>
        public TimeSpan? Timeout { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the transporter should log this message.
        /// </summary>
        public bool ShouldLog { get; set; }

        /// <summary>
        /// Gets or sets the request queue priority.
        /// </summary>
        public QueuePriority Priority { get; set; }

        /// <summary>
        /// Gets or sets the request cancellation token.
        /// </summary>
        public CancellationToken CancellationToken { get; set; }
    }
}
