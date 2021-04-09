using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Resonance
{
    /// <summary>
    /// Represents a resonance message encoding/decoding encryption configuration.
    /// </summary>
    public class ResonanceEncryptionConfiguration
    {
        /// <summary>
        /// Gets a value indicating whether encryption is currently active.
        /// </summary>
        public bool Enabled { get; private set; }

        /// <summary>
        /// Gets or sets the symmetric algorithm used for encryption decryption.
        /// </summary>
        public SymmetricAlgorithm SymmetricAlgorithm { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResonanceEncryptionConfiguration"/> class.
        /// </summary>
        public ResonanceEncryptionConfiguration()
        {
            SymmetricAlgorithm = Rijndael.Create();
        }

        /// <summary>
        /// Sets the <see cref="SymmetricAlgorithm"/> Key and IV from the specified password.
        /// </summary>
        /// <param name="password">The password.</param>
        public void EnableEncryption(String password)
        {
            Enabled = true;
            PasswordDeriveBytes pdb = new PasswordDeriveBytes(password, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
            SymmetricAlgorithm.Key = pdb.GetBytes(32);
            SymmetricAlgorithm.IV = pdb.GetBytes(16);
        }
    }
}
