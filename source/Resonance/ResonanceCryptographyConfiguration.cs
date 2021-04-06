using Resonance.Cryptography;
using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance
{
    /// <summary>
    /// Represents the <see cref="IResonanceTransporter"/> encryption configuration to be passed to its <see cref="IResonanceHeaderTranscoder"/>.
    /// </summary>
    public class ResonanceCryptographyConfiguration
    {
        /// <summary>
        /// Gets or sets a value indicating whether to enable encryption.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets or sets the cryptography provider.
        /// </summary>
        public IResonanceCryptographyProvider CryptographyProvider { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResonanceCryptographyConfiguration"/> class.
        /// </summary>
        public ResonanceCryptographyConfiguration()
        {
            CryptographyProvider = new RSACryptographyProvider();
        }
    }
}
