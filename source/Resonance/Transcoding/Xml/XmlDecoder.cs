using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace Resonance.Transcoding.Xml
{
    public class XmlDecoder : ResonanceDecoder
    {
        protected override object Decode(MemoryStream stream)
        {
            using (BinaryReader reader = new BinaryReader(stream))
            {
                String asmQualifiedName = reader.ReadString();
                Type messageType = Type.GetType(asmQualifiedName);
                XmlSerializer f = XmlSerializer.FromTypes(new[] { messageType })[0]; //Microsoft bug workaround
                return f.Deserialize(stream);
            }
        }

        public override void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
