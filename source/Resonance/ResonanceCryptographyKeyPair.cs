using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance
{
    /// <summary>
    /// Represents a pair of asymmetric encryption keys.
    /// </summary>
    public class ResonanceCryptographyKeyPair
    {
        /// <summary>
        /// Gets or sets the public key.
        /// </summary>
        public String PublicKey { get; set; }

        /// <summary>
        /// Gets or sets the private key.
        /// </summary>
        public String PrivateKey { get; set; }
    }
}
