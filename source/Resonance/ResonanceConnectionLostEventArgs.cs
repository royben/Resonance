using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance
{
    /// <summary>
    /// Represents an <see cref="IResonanceTransporter.ConnectionLost"/> event arguments.
    /// </summary>
    /// <seealso cref="System.EventArgs" />
    public class ResonanceConnectionLostEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the reason for the connection loss.
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// Gets or sets a value indicating whether fail the transporter after this loss of connection.
        /// </summary>
        public bool FailTransporter { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResonanceConnectionLostEventArgs"/> class.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <param name="failTransporter">sets a value indicating whether fail the transporter after this loss of connection.</param>
        public ResonanceConnectionLostEventArgs(Exception exception, bool failTransporter)
        {
            Exception = exception;
            FailTransporter = failTransporter;
        }
    }
}
