using SIPSorcery.Net;
using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance.WebRTC.Messages
{
    public class WebRTCOfferResponse
    {
        public RTCSessionDescriptionInit Answer { get; set; }
    }
}
