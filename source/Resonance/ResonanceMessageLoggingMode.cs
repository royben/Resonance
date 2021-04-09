using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance
{
    /// <summary>
    /// Represents the resonance message logging modes.
    /// </summary>
    public enum ResonanceMessageLoggingMode
    {
        /// <summary>
        /// Will not log the message.
        /// </summary>
        None,
        /// <summary>
        /// Will log the message name.
        /// </summary>
        Title,
        /// <summary>
        /// Will log the message name and contents.
        /// </summary>
        Content,
    }
}
