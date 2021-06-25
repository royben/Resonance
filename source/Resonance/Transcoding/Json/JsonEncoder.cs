using Newtonsoft.Json;
using Resonance.RPC;
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
        private JsonSerializerSettings _primitiveSettings;

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
            Settings.TypeNameHandling = TypeNameHandling.All;

            _primitiveSettings = new JsonSerializerSettings();
            _primitiveSettings.TypeNameHandling = TypeNameHandling.All;
            _primitiveSettings.Converters.Insert(0, new PrimitiveJsonConverter());
        }

        /// <summary>
        /// Encodes the specified message using the specified writer.
        /// </summary>
        /// <param name="writer">The binary writer.</param>
        /// <param name="message">The message.</param>
        protected override void Encode(BinaryWriter writer, object message)
        {
            String json = String.Empty;

            if ((message != null && message.GetType().IsPrimitive) || message is MethodParamCollection)
            {
                json = JsonConvert.SerializeObject(message, _primitiveSettings);
            }
            else
            {
                json = JsonConvert.SerializeObject(message, Settings);
            }

            writer.Write(json);
        }
    }
}
