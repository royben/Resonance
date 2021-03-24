using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance
{
    /// <summary>
    /// Represents an <see cref="IResonanceTransporter.ResponseFailed"/> event arguments.
    /// </summary>
    /// <seealso cref="Resonance.ResonanceResponseEventArgs" />
    public class ResonanceResponseFailedEventArgs : ResonanceResponseEventArgs
    {
        /// <summary>
        /// Gets or sets the failed exception.
        /// </summary>
        public Exception Exception { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResonanceResponseFailedEventArgs"/> class.
        /// </summary>
        /// <param name="response">The response.</param>
        /// <param name="exception">The exception.</param>
        public ResonanceResponseFailedEventArgs(ResonanceResponse response, Exception exception) : base(response)
        {
            Exception = exception;
        }
    }
}
