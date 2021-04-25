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
        /// Gets or sets the request transporter.
        /// </summary>
        public IResonanceTransporter Transporter { get; }

        /// <summary>
        /// Gets or sets the request.
        /// </summary>
        public ResonanceRequest Request { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResonanceRequestEventArgs"/> class.
        /// </summary>
        /// <param name="transporter">The request transporter</param>
        /// <param name="request">The request.</param>
        public ResonanceRequestEventArgs(IResonanceTransporter transporter, ResonanceRequest request)
        {
            Transporter = transporter;
            Request = request;
        }
    }
}
