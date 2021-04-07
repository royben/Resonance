using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace Resonance.Transcoding.Xml
{
    /// <summary>
    /// Represents a Resonance XML message encoder.
    /// </summary>
    /// <seealso cref="Resonance.ResonanceDecoder" />
    [ResonanceTranscoding("xml")]
    public class XmlDecoder : ResonanceDecoder
    {
        /// <summary>
        /// Decodes a message from the specified memory stream.
        /// </summary>
        /// <param name="stream">The memory stream.</param>
        /// <returns></returns>
        public override object Decode(MemoryStream stream)
        {
            using (BinaryReader reader = new BinaryReader(stream))
            {
                String asmQualifiedName = reader.ReadString();
                Type messageType = Type.GetType(asmQualifiedName);
                XmlSerializer f = XmlSerializer.FromTypes(new[] { messageType })[0]; //Microsoft bug workaround
                return f.Deserialize(stream);
            }
        }
    }
}
