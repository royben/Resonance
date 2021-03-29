using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace Resonance.Transcoding.Xml
{
    /// <summary>
    /// Represents a Resonance XML message decoder.
    /// </summary>
    /// <seealso cref="Resonance.ResonanceEncoder" />
    [ResonanceTranscoding("xml")]
    public class XmlEncoder : ResonanceEncoder
    {
        /// <summary>
        /// Encodes the specified message using the specified writer.
        /// </summary>
        /// <param name="writer">The binary writer.</param>
        /// <param name="message">The message.</param>
        protected override void Encode(BinaryWriter writer, object message)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                XmlSerializer f = XmlSerializer.FromTypes(new[] { message.GetType() })[0]; //Microsoft bug workaround.
                f.Serialize(ms, message);

                writer.Write(message.GetType().AssemblyQualifiedName);
                writer.Write(ms.ToArray());
            }
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        public override void Dispose()
        {
            
        }
    }
}
