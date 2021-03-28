using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Resonance.Transcoding.Bson
{
    /// <summary>
    /// Represents a Bson Resonance encoder.
    /// </summary>
    /// <seealso cref="Resonance.ResonanceEncoder" />
    public class BsonEncoder : ResonanceEncoder
    {
        private static JsonSerializer _serializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="BsonEncoder"/> class.
        /// </summary>
        public BsonEncoder()
        {
            _serializer = new BsonSerializerWithUTC();
            _serializer.TypeNameHandling = TypeNameHandling.Objects;
        }

        /// <summary>
        /// Encodes the specified message using the specified writer.
        /// </summary>
        /// <param name="writer">The binary writer.</param>
        /// <param name="message">The message.</param>
        protected override void Encode(BinaryWriter writer, object message)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (BsonDataWriter bsonWriter = new BsonDataWriter(ms))
                {
                    _serializer.Serialize(bsonWriter, message);
                    writer.Write(ms.ToArray());
                }
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
