using Resonance.ExtensionMethods;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Resonance.HandShake
{
    /// <summary>
    /// Represents the default <see cref="IResonanceHandShakeTranscoder"/> implementation.
    /// </summary>
    /// <seealso cref="Resonance.HandShake.IResonanceHandShakeTranscoder" />
    public class ResonanceDefaultHandShakeTranscoder : IResonanceHandShakeTranscoder
    {
        /// <summary>
        /// Encodes the specified handshake message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns></returns>
        public byte[] Encode(ResonanceHandShakeMessage message)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(ms))
                {
                    writer.Write((byte)message.Type);
                    writer.Write(message.ClientId);
                    writer.Write(message.RequireEncryption);

                    if (message.RequireEncryption)
                    {
                        writer.Write(message.EncryptionPublicKey.ToStringOrEmpty());
                        writer.Write(message.SymmetricPassword.ToStringOrEmpty());
                    }
                }

                return ms.ToArray();
            }
        }

        /// <summary>
        /// Decodes the raw data to a handshake message.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        public ResonanceHandShakeMessage Decode(byte[] data)
        {
            using (MemoryStream ms = new MemoryStream(data))
            {
                using (BinaryReader reader = new BinaryReader(ms))
                {
                    var message = new ResonanceHandShakeMessage();

                    message.Type = (ResonanceHandShakeMessageType)reader.ReadByte();
                    message.ClientId = reader.ReadInt32();
                    message.RequireEncryption = reader.ReadBoolean();

                    if (message.RequireEncryption)
                    {
                        message.EncryptionPublicKey = reader.ReadString().ToNullIfEmpty();
                        message.SymmetricPassword = reader.ReadString().ToNullIfEmpty();
                    }

                    return message;
                }
            }
        }
    }
}
