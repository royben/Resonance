using SIPSorcery.Net;
using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance.WebRTC.Messages
{
    public class WebRTCIceCandidateRequest
    {
        public String Candidate { get; set; }
        public String SdpMid { get; set; }
        public ushort SdpMLineIndex { get; set; }
        public String UserNameFragment { get; set; }
    }
}
