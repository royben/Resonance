using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance.HandShake
{
    /// <summary>
    /// Represents a <see cref="ResonanceHandShakeMessage"/> encoder/decoder.
    /// <seealso cref="IResonanceHandShakeNegotiator"/>.
    /// </summary>
    public interface IResonanceHandShakeTranscoder
    {
        /// <summary>
        /// Encodes the specified handshake message.
        /// </summary>
        /// <param name="message">The message.</param>
        byte[] Encode(ResonanceHandShakeMessage message);

        /// <summary>
        /// Decodes the raw data to a handshake message.
        /// </summary>
        /// <param name="data">The data.</param>
        ResonanceHandShakeMessage Decode(byte[] data);
    }
}
