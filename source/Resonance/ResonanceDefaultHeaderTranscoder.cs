using Resonance.ExtensionMethods;
using Resonance.RPC;
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
            writer.Write(ProtocolVersion); //None zero byte must be written here to not confuse with Handshake messages !
            writer.WriteShortASCII(info.Transcoding);
            writer.Write(info.IsCompressed);
            writer.WriteShortASCII(info.Token);
            writer.Write((byte)info.Type);

            if (info.RPCSignature != null)
            {
                writer.WriteShortASCII(info.RPCSignature.ToString());
            }
            else
            {
                writer.WriteShortASCII(String.Empty);
            }

            writer.Write((byte)(info.Timeout ?? 0));


            if (info.Type == ResonanceTranscodingInformationType.Response || info.Type == ResonanceTranscodingInformationType.MessageSyncACK)
            {
                writer.Write(info.Completed);
                writer.Write(info.HasError);
                writer.WriteUTF8(info.ErrorMessage);
            }
            else if (info.Type == ResonanceTranscodingInformationType.Disconnect)
            {
                writer.WriteUTF8(info.ErrorMessage);
            }

            writer.Write((uint)writer.BaseStream.Position + sizeof(uint)); //Increase size when adding fields.
            //Add new fields here...
        }

        /// <summary>
        /// Decodes the header using the specified binary reader and populates the specified decoding information.
        /// </summary>
        /// <param name="reader">The binary reader.</param>
        /// <param name="info">The decoding information.</param>
        public virtual void Decode(BinaryReader reader, ResonanceDecodingInformation info)
        {
            info.ProtocolVersion = reader.ReadByte();
            info.Transcoding = reader.ReadShortASCII();
            info.IsCompressed = reader.ReadBoolean();
            info.Token = reader.ReadShortASCII();
            info.Type = (ResonanceTranscodingInformationType)reader.ReadByte();

            String rpcSignatureString = reader.ReadShortASCII();

            if (!String.IsNullOrEmpty(rpcSignatureString))
            {
                info.RPCSignature = RPCSignature.FromString(rpcSignatureString);
            }

            byte timeout = reader.ReadByte();

            if (timeout > 0)
            {
                info.Timeout = timeout;
            }

            if (info.Type == ResonanceTranscodingInformationType.Response || info.Type == ResonanceTranscodingInformationType.MessageSyncACK)
            {
                info.Completed = reader.ReadBoolean();
                info.HasError = reader.ReadBoolean();
                info.ErrorMessage = reader.ReadUTF8();
            }
            else if (info.Type == ResonanceTranscodingInformationType.Disconnect)
            {
                info.ErrorMessage = reader.ReadUTF8();
            }

            info.ActualMessageStreamPosition = reader.ReadUInt32();

            if (info.ProtocolVersion >= ProtocolVersion)
            {
                //Add new fields here...
            }
        }
    }
}
