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
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets or sets the KeepAlive request interval.
        /// </summary>
        public TimeSpan Interval { get; set; }

        /// <summary>
        /// Gets or sets the keep alive retries.
        /// </summary>
        public uint Retries { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to auto respond to keep alive requests.
        /// </summary>
        public bool EnableAutoResponse { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the transporter should fail if the keep alive has reached the given timeout and retries.
        /// </summary>
        public bool FailTransporterOnTimeout { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResonanceKeepAliveConfiguration"/> class.
        /// </summary>
        public ResonanceKeepAliveConfiguration()
        {
            EnableAutoResponse = true;
            Interval = TimeSpan.FromSeconds(2);
            FailTransporterOnTimeout = true;
            Retries = 5;
        }

        /// <summary>
        /// Clones this instance.
        /// </summary>
        /// <returns></returns>
        public ResonanceKeepAliveConfiguration Clone()
        {
            return this.MemberwiseClone() as ResonanceKeepAliveConfiguration;
        }
    }
}
