using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance
{
    /// <summary>
    /// Represents an <see cref="IResonanceTransporter.RequestFailed"/> event arguments.
    /// </summary>
    /// <seealso cref="Resonance.ResonanceRequestEventArgs" />
    public class ResonanceRequestFailedEventArgs : ResonanceRequestEventArgs
    {
        /// <summary>
        /// Gets or sets the failed exception.
        /// </summary>
        public Exception Exception { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResonanceRequestFailedEventArgs"/> class.
        /// </summary>
        /// <param name="transporter">The request transporter.</param>
        /// <param name="request">The request.</param>
        /// <param name="exception">The exception.</param>
        public ResonanceRequestFailedEventArgs(IResonanceTransporter transporter, ResonanceRequest request, Exception exception) : base(transporter, request)
        {
            Exception = exception;
        }
    }
}
