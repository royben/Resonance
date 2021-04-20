using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance.WebRTC.Exceptions
{
    public class ResonanceWebRTCChannelClosedException : Exception
    {
        public ResonanceWebRTCChannelClosedException() : base("The WebRTC data channel has closed unexpectedly.")
        {

        }
    }
}
