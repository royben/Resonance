using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Resonance
{
    /// <summary>
    /// Represents a registration of an outgoing response message.
    /// </summary>
    public class ResonancePendingResponse
    {
        /// <summary>
        /// Gets or sets the response.
        /// </summary>
        public ResonanceResponse Response { get; set; }

        /// <summary>
        /// Gets or sets the response completion source.
        /// </summary>
        public TaskCompletionSource<Object> CompletionSource { get; set; }

        /// <summary>
        /// Gets or sets the response configuration.
        /// </summary>
        public ResonanceResponseConfig Config { get; set; }
    }
}
