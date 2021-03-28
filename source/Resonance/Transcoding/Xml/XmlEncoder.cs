using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace Resonance.Transcoding.Xml
{
    public class XmlEncoder : ResonanceEncoder
    {
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

        public override void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
