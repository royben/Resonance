using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance.WebRTC
{
    /// <summary>
    /// Represents a WebRTC ICE server (Stun/Turn).
    /// </summary>
    public class WebRTCIceServer
    {
        /// <summary>
        /// Gets or sets the server URL.
        /// </summary>
        public String Url { get; set; }

        /// <summary>
        /// Gets or sets an optional user name.
        /// </summary>
        public String UserName { get; set; }

        /// <summary>
        /// Gets or sets an optional user credentials.
        /// </summary>
        public String Credentials { get; set; }
    }
}
