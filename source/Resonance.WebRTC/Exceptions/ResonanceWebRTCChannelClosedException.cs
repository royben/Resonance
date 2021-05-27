using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance.WebRTC.Exceptions
{
    /// <summary>
    /// Represents an exception that might occur when a WebRTC adapter has failed unexpectedly.
    /// </summary>
    /// <seealso cref="System.Exception" />
    public class ResonanceWebRTCChannelClosedException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ResonanceWebRTCChannelClosedException"/> class.
        /// </summary>
        public ResonanceWebRTCChannelClosedException() : base("The data channel has closed unexpectedly.")
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResonanceWebRTCChannelClosedException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public ResonanceWebRTCChannelClosedException(String message) : base(message)
        {

        }
    }
}
