using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance
{
    /// <summary>
    /// Represents a Resonance request handler response.
    /// </summary>
    public interface IResonanceActionResult
    {
        /// <summary>
        /// Gets or sets the response message.
        /// </summary>
        Object Response { get; set; }

        /// <summary>
        /// Gets the response configuration.
        /// </summary>
        ResonanceResponseConfig Config { get; set; }
    }
}
