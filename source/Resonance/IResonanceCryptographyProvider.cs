using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance
{
    /// <summary>
    /// Represents an asymmetric encryption/decryption methods.
    /// </summary>
    public interface IResonanceCryptographyProvider
    {
        /// <summary>
        /// Gets or sets the key size used to generate the keys.
        /// </summary>
        int KeySize { get; set; }

        /// <summary>
        /// Returns a pair of public and private keys.
        /// </summary>
        /// <returns></returns>
        ResonanceCryptographyKeyPair CreateKeys();

        /// <summary>
        /// Encrypts the specified text using a public key.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="publicKey">The public key.</param>
        /// <returns></returns>
        String Encrypt(String text, String publicKey);

        /// <summary>
        /// Decrypts the specified text using a private key.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="privateKey">The private key.</param>
        /// <returns></returns>
        String Decrypt(String text, String privateKey);
    }
}
