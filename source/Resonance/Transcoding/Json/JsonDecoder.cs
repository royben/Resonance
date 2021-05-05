using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Resonance.Transcoding.Json
{
    /// <summary>
    /// Represents a Json Resonance message decoder.
    /// </summary>
    /// <seealso cref="Resonance.ResonanceDecoder" />
    [ResonanceTranscoding("json")]
    public class JsonDecoder : ResonanceDecoder
    {
        /// <summary>
        /// Gets the Json settings.
        /// </summary>
        public JsonSerializerSettings Settings { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonDecoder"/> class.
        /// </summary>
        public JsonDecoder()
        {
            Settings = new JsonSerializerSettings();
            Settings.TypeNameHandling = TypeNameHandling.Objects;
        }

        /// <summary>
        /// Decodes a message from the specified memory stream.
        /// </summary>
        /// <param name="stream">The memory stream.</param>
        /// <returns></returns>
        public override object Decode(MemoryStream stream)
        {
            using (BinaryReader reader = new BinaryReader(stream))
            {
                String json = reader.ReadString();
                object obj = JsonConvert.DeserializeObject(json, Settings);
                return obj;
            }
        }
    }
}
