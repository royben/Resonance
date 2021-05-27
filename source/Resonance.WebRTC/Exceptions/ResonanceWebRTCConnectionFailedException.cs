using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance.WebRTC.Exceptions
{
    /// <summary>
    /// Represents an exception that might occur while trying to connect a WebRTC adapter.
    /// </summary>
    /// <seealso cref="System.Exception" />
    public class ResonanceWebRTCConnectionFailedException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ResonanceWebRTCConnectionFailedException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public ResonanceWebRTCConnectionFailedException(String message) : base(message)
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResonanceWebRTCConnectionFailedException"/> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
        public ResonanceWebRTCConnectionFailedException(String message, Exception innerException) : base(message, innerException)
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResonanceWebRTCConnectionFailedException"/> class.
        /// </summary>
        /// <param name="innerException">The inner exception.</param>
        public ResonanceWebRTCConnectionFailedException(Exception innerException) : base($"Could not establish the WebRTC connection. {innerException.Message}", innerException)
        {

        }
    }
}
