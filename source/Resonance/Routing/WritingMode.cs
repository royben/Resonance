using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance.Routing
{
    /// <summary>
    /// Represents a <see cref="TransporterRouter"/> data writing mode.
    /// </summary>
    public enum WritingMode
    {
        /// <summary>
        /// Uses the internal transporters mechanism to submit the data.
        /// </summary>
        Standard,
        /// <summary>
        /// Writes data directly to the transporter adapter.
        /// </summary>
        AdapterDirect
    }
}
