using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Resonance.Cryptography
{
    /// <summary>
    /// Represents an asymmetric RSA public key encryption/decryption provider.
    /// </summary>
    /// <seealso cref="Resonance.IResonanceCryptographyProvider" />
    public class RSACryptographyProvider : IResonanceCryptographyProvider
    {
        /// <summary>
        /// Gets or sets the key size used to generate the keys.
        /// </summary>
        public int KeySize { get; set; } = 1024;

        /// <summary>
        /// Returns a pair of public and private keys.
        /// </summary>
        /// <returns></returns>
        public ResonanceCryptographyKeyPair CreateKeys()
        {
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
            {
                rsa.KeySize = KeySize;

                return new ResonanceCryptographyKeyPair()
                {
                    PublicKey = rsa.ToXmlString(false),
                    PrivateKey = rsa.ToXmlString(true)
                };
            }
        }

        /// <summary>
        /// Encrypts the specified text using a public key.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="publicKey">The public key.</param>
        /// <returns></returns>
        public String Encrypt(String text, string publicKey)
        {
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
            {
                rsa.KeySize = KeySize;

                rsa.FromXmlString(publicKey);
                return Convert.ToBase64String(rsa.Encrypt(Encoding.ASCII.GetBytes(text), true));
            }
        }

        /// <summary>
        /// Decrypts the specified text using a private key.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="privateKey">The private key.</param>
        /// <returns></returns>
        /// <exception cref="Exception">The key provided is a public key and does not contain the private key elements required for decryption</exception>
        public String Decrypt(String text, string privateKey)
        {
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
            {
                rsa.KeySize = KeySize;

                rsa.FromXmlString(privateKey);

                if (rsa.PublicOnly)
                    throw new Exception("The key provided is a public key and does not contain the private key elements required for decryption");

                return Encoding.ASCII.GetString(rsa.Decrypt(Convert.FromBase64String(text), true));
            }
        }
    }
}
