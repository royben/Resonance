using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Resonance
{
    /// <summary>
    /// Represents a continuous request configuration.
    /// </summary>
    /// <seealso cref="Resonance.ResonanceRequestConfig" />
    public class ResonanceContinuousRequestConfig : ResonanceRequestConfig
    {
        /// <summary>
        /// Gets or sets an optional timeout to use between each response.
        /// </summary>
        public TimeSpan? ContinuousTimeout { get; set; }
    }
}
