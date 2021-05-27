using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance.WebRTC.Messages
{
    /// <summary>
    /// Represents a base WebRTC signaling message.
    /// </summary>
    public abstract class WebRTCMessage
    {
        /// <summary>
        /// This value is used to identity the adapter when multiple adapters are using the same signaling transporter, 
        /// and must match between the connecting and the accepting transporter.
        /// When using one adapter per signaling transporter there is no need to change this value.
        /// The default value is "resonance".
        /// </summary>
        public String ChannelName { get; set; }
    }
}
