using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Resonance.Transcoding.Bson
{
    public class BsonEncoder : ResonanceEncoder
    {
        private static JsonSerializer _serializer;

        public BsonEncoder()
        {
            _serializer = new BsonSerializerWithUTC();
        }

        protected override void Encode(BinaryWriter writer, object message)
        {
            MemoryStream ms = new MemoryStream();
            using (BsonWriter writer = new BsonWriter(ms))
            {
                _serializer.Serialize(writer, obj);
                return ms.ToArray();
            }
        }

        public override void Dispose()
        {
            
        }
    }
}
