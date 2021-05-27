using SIPSorcery.Net;
using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance.WebRTC.Messages
{
    /// <summary>
    /// Represents a WebRTC offer response.
    /// </summary>
    /// <seealso cref="Resonance.WebRTC.Messages.WebRTCMessage" />
    public class WebRTCOfferResponse : WebRTCMessage
    {
        /// <summary>
        /// Gets or sets the answer session description.
        /// </summary>
        public WebRTCSessionDescription Answer { get; set; }
    }
}
