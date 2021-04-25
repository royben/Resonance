using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance
{
    /// <summary>
    /// Represents a basic <see cref="ResonanceResponse"/> event arguments.
    /// </summary>
    /// <seealso cref="System.EventArgs" />
    public class ResonanceResponseEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the response transporter.
        /// </summary>
        public IResonanceTransporter Transporter { get; }

        /// <summary>
        /// Gets or sets the response.
        /// </summary>
        public ResonanceResponse Response { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResonanceResponseEventArgs"/> class.
        /// </summary>
        /// <param name="transporter">The response transporter.</param>
        /// <param name="response">The response.</param>
        public ResonanceResponseEventArgs(IResonanceTransporter transporter, ResonanceResponse response)
        {
            Transporter = transporter;
            Response = response;
        }
    }
}
