using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance
{
    /// <summary>
    /// Represents a basic <see cref="ResonanceRequest"/> event arguments.
    /// </summary>
    /// <seealso cref="System.EventArgs" />
    public class ResonanceRequestEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the request.
        /// </summary>
        public ResonanceRequest Request { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResonanceRequestEventArgs"/> class.
        /// </summary>
        /// <param name="request">The request.</param>
        public ResonanceRequestEventArgs(ResonanceRequest request)
        {
            Request = request;
        }
    }
}
