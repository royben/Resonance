using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance
{
    /// <summary>
    /// Represents a keep alive mechanism configuration.
    /// </summary>
    public class ResonanceKeepAliveConfiguration
    {
        /// <summary>
        /// Gets or sets a value indicating whether to use the keep alive mechanism.
        /// </summary>
        bool Enable { get; set; }

        /// <summary>
        /// Gets or sets the timeout to fail this transporter.
        /// </summary>
        TimeSpan Timeout { get; set; }

        /// <summary>
        /// Gets or sets the keep alive retries.
        /// </summary>
        int Retries { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to auto respond to keep alive requests.
        /// </summary>
        bool EnableAutoResponse { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResonanceKeepAliveConfiguration"/> class.
        /// </summary>
        public ResonanceKeepAliveConfiguration()
        {
            EnableAutoResponse = true;
            Timeout = TimeSpan.FromSeconds(2);
            Retries = 5;
        }
    }
}
