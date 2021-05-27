using SIPSorcery.Net;
using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance.WebRTC.Messages
{
    /// <summary>
    /// Represents a WebRTC ICE candidate request message.
    /// </summary>
    /// <seealso cref="Resonance.WebRTC.Messages.WebRTCMessage" />
    public class WebRTCIceCandidateRequest : WebRTCMessage
    {
        /// <summary>
        /// Gets or sets the candidate.
        /// </summary>
        public WebRTCIceCandidate Candidate { get; set; }
    }
}
