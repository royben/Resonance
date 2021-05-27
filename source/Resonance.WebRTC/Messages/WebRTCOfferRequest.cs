using SIPSorcery.Net;
using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance.WebRTC.Messages
{
    /// <summary>
    /// Represents a WebRTC offer request.
    /// </summary>
    /// <seealso cref="Resonance.WebRTC.Messages.WebRTCMessage" />
    public class WebRTCOfferRequest : WebRTCMessage
    {
        /// <summary>
        /// Gets or sets the offer session description.
        /// </summary>
        public WebRTCSessionDescription Offer { get; set; }
    }
}
