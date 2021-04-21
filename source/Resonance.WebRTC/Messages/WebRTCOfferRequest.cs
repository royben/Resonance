using SIPSorcery.Net;
using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance.WebRTC.Messages
{
    public class WebRTCOfferRequest
    {
        public WebRTCSessionDescription Offer { get; set; }

        public WebRTCOfferRequest()
        {
            Offer = new WebRTCSessionDescription();
        }
    }
}
