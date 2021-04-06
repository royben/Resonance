using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance.HandShake
{
    /// <summary>
    /// Represents a resonance handshake message.
    /// </summary>
    public class ResonanceHandShakeMessage
    {
        /// <summary>
        /// Gets or sets the message type.
        /// </summary>
        public ResonanceHandShakeMessageType Type { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="ResonanceDefaultHandShakeNegotiator"/> client id.
        /// </summary>
        public int ClientId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the sender requires a secure channel.
        /// </summary>
        public bool RequireEncryption { get; set; }

        /// <summary>
        /// Gets or sets the encryption public key.
        /// </summary>
        public String EncryptionPublicKey { get; set; }

        /// <summary>
        /// Gets or sets the symmetric password when available.
        /// This field should be RSA encrypted.
        /// </summary>
        public String SymmetricPassword { get; set; }
    }
}
