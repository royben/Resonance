using SIPSorcery.Net;
using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance.WebRTC
{
    /// <summary>
    /// Represents a WebRTC session description.
    /// </summary>
    public class WebRTCSessionDescription
    {
        /// <summary>
        /// Gets or sets the session description.
        /// </summary>
        public String Sdp { get; set; }

        /// <summary>
        /// Gets or sets the session description type.
        /// </summary>
        public String Type { get; set; }

        internal RTCSdpType InternalType
        {
            get { return (RTCSdpType)Enum.Parse(typeof(RTCSdpType), Type); }
            set
            {
                Type = value.ToString();
            }
        }

        internal static WebRTCSessionDescription FromSessionDescription(RTCSessionDescriptionInit sessionDescription)
        {
            return new WebRTCSessionDescription()
            {
                InternalType = sessionDescription.type,
                Sdp = sessionDescription.sdp
            };
        }

        internal RTCSessionDescriptionInit ToSessionDescription()
        {
            return new RTCSessionDescriptionInit() { sdp = Sdp, type = InternalType };
        }
    }
}
