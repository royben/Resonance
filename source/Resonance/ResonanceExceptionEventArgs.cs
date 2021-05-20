using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance
{
    /// <summary>
    /// Represents an exception event arguments.
    /// </summary>
    /// <seealso cref="System.EventArgs" />
    public class ResonanceExceptionEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the exception.
        /// </summary>
        public Exception Exception { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResonanceExceptionEventArgs"/> class.
        /// </summary>
        /// <param name="exception">The exception.</param>
        public ResonanceExceptionEventArgs(Exception exception)
        {
            Exception = exception;
        }
    }
}
