using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Resonance.Transcoding.Bson
{
    public class BsonDecoder : ResonanceDecoder
    {
        private static JsonSerializer _serializer;

        public BsonDecoder()
        {
            _serializer = new BsonSerializerWithUTC();
        }

        protected override object Decode(BinaryReader reader)
        {
            throw new NotImplementedException();
        }

        public override void Dispose()
        {
            
        }
    }
}
