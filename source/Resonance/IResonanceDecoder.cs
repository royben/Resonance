using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance
{
    /// <summary>
    /// Represents a Resonance decoder capable of decoding data received by an <see cref="IResonanceAdapter"/>.
    /// </summary>
    /// <seealso cref="Resonance.IResonanceComponent" />
    public interface IResonanceDecoder : IResonanceComponent
    {
        /// <summary>
        /// Gets or sets the message compression configuration.
        /// </summary>
        ResonanceCompressionConfiguration CompressionConfiguration { get; }

        /// <summary>
        /// Gets the encryption configuration.
        /// </summary>
        ResonanceEncryptionConfiguration EncryptionConfiguration { get; }

        /// <summary>
        /// Decodes the specified data and populates the specified decoding information.
        /// </summary>
        /// <param name="data">The encoded data.</param>
        /// <param name="info">The decoding information object to populate.</param>
        void Decode(byte[] data, ResonanceDecodingInformation info);
    }
}
