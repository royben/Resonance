using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance
{
    /// <summary>
    /// Represents a registration of an awaiting request.
    /// </summary>
    public interface IResonancePendingRequest
    {
        /// <summary>
        /// Gets or sets the Resonance request.
        /// </summary>
        ResonanceRequest Request { get; set; }
    }
}
