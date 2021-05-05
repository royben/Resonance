using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance
{
    /// <summary>
    /// Represents a registration of an awaiting request.
    /// </summary>
    public interface IResonancePendingMessage
    {
        /// <summary>
        /// Gets or sets the Resonance message.
        /// </summary>
        ResonanceMessage Message { get; set; }
    }
}
