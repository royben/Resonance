using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance
{
    /// <summary>
    /// Represents a Resonance encoder capable of encoding <see cref="ResonanceEncodingInformation"/> to a byte array.
    /// </summary>
    /// <seealso cref="Resonance.IResonanceComponent" />
    public interface IResonanceEncoder : IResonanceComponent
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
        /// Encodes the specified encoding information.
        /// </summary>
        /// <param name="info">The encoding information.</param>
        /// <returns></returns>
        byte[] Encode(ResonanceEncodingInformation info);
    }
}
