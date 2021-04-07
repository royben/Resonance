using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance.Logging
{
    /// <summary>
    /// Represents the <see cref="ResonanceLogManager.LogItemAvailable"/> event arguments.
    /// </summary>
    /// <seealso cref="System.EventArgs" />
    public class ResonanceLogItemAvailableEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the log item.
        /// </summary>
        public ResonanceLogItem LogItem { get; set; }
    }
}
