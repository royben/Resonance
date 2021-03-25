using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance
{
    /// <summary>
    /// Represents an <see cref="IResonanceAdapter"/> stream writing mode.
    /// </summary>
    public enum ResonanceAdapterWriteMode
    {
        /// <summary>
        /// Write to the stream immediatly.
        /// </summary>
        Direct,
        /// <summary>
        /// Queues data and writes the entire queue on each cycle. 
        /// </summary>
        Queue
    }
}
