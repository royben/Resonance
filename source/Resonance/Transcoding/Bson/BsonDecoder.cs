using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Resonance.Transcoding.Bson
{
    /// <summary>
    /// Represents a Bson Resonance decoder.
    /// </summary>
    /// <seealso cref="Resonance.ResonanceDecoder" />
    public class BsonDecoder : ResonanceDecoder
    {
        private static JsonSerializer _serializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="BsonDecoder"/> class.
        /// </summary>
        public BsonDecoder()
        {
            _serializer = new BsonSerializerWithUTC();
            _serializer.TypeNameHandling = TypeNameHandling.Objects;
        }

        /// <summary>
        /// Decodes a message from the specified memory stream.
        /// </summary>
        /// <param name="stream">The memory stream.</param>
        /// <returns></returns>
        protected override object Decode(MemoryStream stream)
        {
            using (BsonDataReader reader = new BsonDataReader(stream))
            {
                return _serializer.Deserialize(reader);
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
