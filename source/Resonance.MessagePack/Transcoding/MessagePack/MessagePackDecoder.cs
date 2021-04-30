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
    /// <seealso cref="Resonance.ResonanceDecoder" />
    public class MessagePackDecoder : ResonanceDecoder
    {
        /// <summary>
        /// Decodes a message from the specified memory stream.
        /// </summary>
        /// <param name="stream">The memory stream.</param>
        /// <returns></returns>
        public override object Decode(MemoryStream stream)
        {
            return MessagePackSerializer.Typeless.Deserialize(stream);
        }
    }
}
