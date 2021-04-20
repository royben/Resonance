using SIPSorcery.Net;
using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance.WebRTC.Messages
{
    public class WebRTCOfferRequest
    {
        public RTCSessionDescriptionInit Offer { get; set; }
    }
}
