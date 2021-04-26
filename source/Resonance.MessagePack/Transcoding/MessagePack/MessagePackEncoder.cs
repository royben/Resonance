using MessagePack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Resonance.MessagePack.Transcoding.MessagePack
{
    /// <summary>
    /// Represents a Resonance MessagePack message decoder.
    /// </summary>
    /// <seealso cref="Resonance.ResonanceEncoder" />
    public class MessagePackEncoder : ResonanceEncoder
    {
        /// <summary>
        /// Encodes the specified message using the specified writer.
        /// </summary>
        /// <param name="writer">The binary writer.</param>
        /// <param name="message">The message.</param>
        protected override void Encode(BinaryWriter writer, object message)
        {
            writer.Write(MessagePackSerializer.Typeless.Serialize(message));
        }
    }
}
