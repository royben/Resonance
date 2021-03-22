using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Resonance.Transcoding.Json
{
    public class JsonEncoder : ResonanceEncoder
    {
        private JsonSerializerSettings _settings;

        public JsonEncoder()
        {
            _settings = new JsonSerializerSettings();
            _settings.TypeNameHandling = TypeNameHandling.Objects;
        }

        public override byte[] Encode(ResonanceTranscodingInformation message)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(ms))
                {
                    WriterHeader(message, writer);
                    String json = JsonConvert.SerializeObject(message.Message, _settings);
                    writer.Write(json);

                    return ms.ToArray();
                }
            }
        }

        public override void Dispose()
        {
            //throw new NotImplementedException();
        }
    }
}
