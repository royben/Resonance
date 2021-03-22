using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Resonance.Transcoding.Json
{
    public class JsonDecoder : ResonanceDecoder
    {
        private JsonSerializerSettings _settings;

        public JsonDecoder()
        {
            _settings = new JsonSerializerSettings();
            _settings.TypeNameHandling = TypeNameHandling.Objects;
        }

        public override ResonanceTranscodingInformation Decode(byte[] data)
        {
            ResonanceTranscodingInformation info = new ResonanceTranscodingInformation();

            using (MemoryStream ms = new MemoryStream(data))
            {
                using (BinaryReader reader = new BinaryReader(ms))
                {
                    ReadHeader(info, reader);

                    String json = reader.ReadString();
                    info.Message = JsonConvert.DeserializeObject(json, _settings);

                    return info;
                }
            }
        }

        public override void Dispose()
        {
            //throw new NotImplementedException();
        }
    }
}
