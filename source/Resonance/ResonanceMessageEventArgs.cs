using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance
{
    /// <summary>
    /// Represents a basic <see cref="ResonanceMessage"/> event arguments.
    /// </summary>
    /// <seealso cref="System.EventArgs" />
    public class ResonanceMessageEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the message transporter.
        /// </summary>
        public IResonanceTransporter Transporter { get; }

        /// <summary>
        /// Gets or sets the resonance message.
        /// </summary>
        public ResonanceMessage Message { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResonanceMessageEventArgs"/> class.
        /// </summary>
        /// <param name="transporter">The request transporter</param>
        /// <param name="message">The resonance message.</param>
        public ResonanceMessageEventArgs(IResonanceTransporter transporter, ResonanceMessage message)
        {
            Transporter = transporter;
            Message = message;
        }
    }
}
