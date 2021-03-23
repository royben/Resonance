﻿using Newtonsoft.Json;
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

        public override void Decode(byte[] data, ResonanceDecodingInformation info)
        {
            using (MemoryStream ms = new MemoryStream(data))
            {
                using (BinaryReader reader = new BinaryReader(ms))
                {
                    ReadHeader(info, reader);

                    String json = reader.ReadString();
                    info.Message = JsonConvert.DeserializeObject(json, _settings);
                }
            }
        }

        public override void Dispose()
        {
            //throw new NotImplementedException();
        }
    }
}
