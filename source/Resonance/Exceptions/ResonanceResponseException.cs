using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance.Exceptions
{
    /// <summary>
    /// Represents an error returned by the remote transporter in response to a message.
    /// </summary>
    public class ResonanceResponseException : Exception
    {
        /// <summary>
        /// Gets the application defined error code provided by the remote transporter.
        /// Zero when no code was specified.
        /// </summary>
        public int ErrorCode { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResonanceResponseException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        public ResonanceResponseException(String message) : this(message, 0)
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResonanceResponseException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="errorCode">An application defined error code. Zero means no code.</param>
        public ResonanceResponseException(String message, int errorCode) : base(message)
        {
            ErrorCode = errorCode;
        }
    }
}
