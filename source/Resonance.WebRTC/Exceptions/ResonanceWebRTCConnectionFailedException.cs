using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance.WebRTC.Exceptions
{
    public class ResonanceWebRTCConnectionFailedException : Exception
    {
        public ResonanceWebRTCConnectionFailedException(String message) : base(message)
        {

        }

        public ResonanceWebRTCConnectionFailedException(String message, Exception innerException) : base(message, innerException)
        {

        }

        public ResonanceWebRTCConnectionFailedException(Exception innerException) : base("Could not establish the WebRTC connection.", innerException)
        {

        }
    }
}
