using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Resonance
{
    /// <summary>
    /// Represents a Resonance protocol header encoder/decoder.
    /// </summary>
    public interface IResonanceHeaderTranscoder
    {
        /// <summary>
        /// Gets the protocol version.
        /// </summary>
        byte ProtocolVersion { get; }

        /// <summary>
        /// Encodes the specified encoding information header using the specified binary writer.
        /// </summary>
        /// <param name="writer">The binary writer.</param>
        /// <param name="info">The encoding information.</param>
        void Encode(BinaryWriter writer, ResonanceEncodingInformation info);

        /// <summary>
        /// Decodes the header using the specified binary reader and populates the specified decoding information.
        /// </summary>
        /// <param name="reader">The binary reader.</param>
        /// <param name="info">The decoding information.</param>
        void Decode(BinaryReader reader, ResonanceDecodingInformation info);
    }
}
