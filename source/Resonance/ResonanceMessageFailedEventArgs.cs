using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance
{
    /// <summary>
    /// Represents a failed message event arguments.
    /// </summary>
    /// <seealso cref="Resonance.ResonanceMessageEventArgs" />
    public class ResonanceMessageFailedEventArgs : ResonanceMessageEventArgs
    {
        /// <summary>
        /// Gets or sets the failed exception.
        /// </summary>
        public Exception Exception { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResonanceMessageFailedEventArgs"/> class.
        /// </summary>
        /// <param name="transporter">The message transporter.</param>
        /// <param name="message">The resonance message.</param>
        /// <param name="exception">The exception.</param>
        public ResonanceMessageFailedEventArgs(IResonanceTransporter transporter, ResonanceMessage message, Exception exception) : base(transporter, message)
        {
            Exception = exception;
        }
    }
}
