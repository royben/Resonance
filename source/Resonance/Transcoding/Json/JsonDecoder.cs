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
        /// Decodes a message using the specified binary reader.
        /// </summary>
        /// <param name="reader">The binary reader.</param>
        /// <returns></returns>
        protected override object Decode(BinaryReader reader)
        {
            String json = reader.ReadString();
            return JsonConvert.DeserializeObject(json, Settings);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        public override void Dispose()
        {
            //throw new NotImplementedException();
        }
    }
}
