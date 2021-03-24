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
        /// Gets or sets the response.
        /// </summary>
        public ResonanceResponse Response { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResonanceResponseEventArgs"/> class.
        /// </summary>
        /// <param name="response">The response.</param>
        public ResonanceResponseEventArgs(ResonanceResponse response)
        {
            Response = response;
        }
    }
}
