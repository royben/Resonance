using SIPSorcery.Net;
using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance.WebRTC.Messages
{
    public class WebRTCOfferResponse
    {
        public WebRTCSessionDescription Answer { get; set; }

        public WebRTCOfferResponse()
        {
            Answer = new WebRTCSessionDescription();
        }
    }
}
