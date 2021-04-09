using Resonance.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance
{
    /// <summary>
    /// Represents a <see cref="ResonanceResponse"/> configuration.
    /// </summary>
    public class ResonanceResponseConfig
    {
        /// <summary>
        /// Gets or sets a value indicating whether the continuous request has completed.
        /// </summary>
        public bool Completed { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the response contains an error.
        /// </summary>
        public bool HasError { get; set; }

        /// <summary>
        /// Gets or sets the response error message.
        /// </summary>
        public String ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the request logging mode.
        /// </summary>
        public ResonanceMessageLoggingMode LoggingMode { get; set; }

        /// <summary>
        /// Gets or sets the response queue priority.
        /// </summary>
        public QueuePriority Priority { get; set; }
    }
}
