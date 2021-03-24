using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Resonance
{
    /// <summary>
    /// Represents the default Resonance protocol header encoder/decoder.
    /// </summary>
    /// <seealso cref="Resonance.IResonanceHeaderTranscoder" />
    public class ResonanceDefaultHeaderTranscoder : IResonanceHeaderTranscoder
    {
        /// <summary>
        /// Gets the protocol version.
        /// </summary>
        public virtual byte ProtocolVersion { get; } = 1; //Increment when appending to protocol header.

        /// <summary>
        /// Encodes the specified encoding information header using the specified binary writer.
        /// </summary>
        /// <param name="writer">The binary writer.</param>
        /// <param name="info">The encoding information.</param>
        public virtual void Encode(BinaryWriter writer, ResonanceEncodingInformation info)
        {
            writer.Write(ProtocolVersion);
            writer.Write(info.IsCompressed);
            writer.Write(info.Token);
            writer.Write((byte)info.Type);
            writer.Write(info.Completed);
            writer.Write(info.HasError);
            writer.Write(info.ErrorMessage ?? String.Empty);
            writer.Write((uint)writer.BaseStream.Position + sizeof(uint));
        }

        /// <summary>
        /// Decodes the header using the specified binary reader and populates the specified decoding information.
        /// </summary>
        /// <param name="reader">The binary reader.</param>
        /// <param name="info">The decoding information.</param>
        public virtual void Decode(BinaryReader reader, ResonanceDecodingInformation info)
        {
            info.ProtocolVersion = reader.ReadByte();
            info.IsCompressed = reader.ReadBoolean();
            info.Token = reader.ReadString();
            info.Type = (ResonanceTranscodingInformationType)reader.ReadByte();
            info.Completed = reader.ReadBoolean();
            info.HasError = reader.ReadBoolean();
            info.ErrorMessage = reader.ReadString();
            info.ActualMessageStreamPosition = reader.ReadUInt32();
        }
    }
}
