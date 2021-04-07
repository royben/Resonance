using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Resonance.Transcoding.Json
{
    /// <summary>
    /// Represents a Json Resonance message encoder.
    /// </summary>
    /// <seealso cref="Resonance.ResonanceEncoder" />
    [ResonanceTranscoding("json")]
    public class JsonEncoder : ResonanceEncoder
    {
        /// <summary>
        /// Gets or sets the Json settings.
        /// </summary>
        public JsonSerializerSettings Settings { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonEncoder"/> class.
        /// </summary>
        public JsonEncoder()
        {
            Settings = new JsonSerializerSettings();
            Settings.TypeNameHandling = TypeNameHandling.Objects;
        }

        /// <summary>
        /// Encodes the specified message using the specified writer.
        /// </summary>
        /// <param name="writer">The binary writer.</param>
        /// <param name="message">The message.</param>
        protected override void Encode(BinaryWriter writer, object message)
        {
            String json = JsonConvert.SerializeObject(message, Settings);
            writer.Write(json);
        }
    }
}
