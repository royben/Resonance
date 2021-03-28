using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Resonance.Transcoding.Bson
{
    /// <summary>
    /// Represents a Bson serializer with support for DateTime "Kind" field.
    /// </summary>
    /// <seealso cref="Newtonsoft.Json.JsonSerializer" />
    internal class BsonSerializerWithUTC : JsonSerializer
    {
        private class DateTimeContractResolver : DefaultContractResolver
        {
            protected override JsonContract CreateContract(Type objectType)
            {
                var contract = base.CreateContract(objectType);

                if (objectType == typeof(DateTime) || objectType == typeof(DateTime?))
                    contract.Converter = new DateTimeConverter();

                return contract;
            }
        }

        private class DateTimeConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return typeof(DateTime) == objectType
                    || typeof(DateTime?) == objectType;
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                var dateTimeOffset = (DateTime)value;
                // Serialize DateTimeOffset as round-trip formatted string
                serializer.Serialize(writer, dateTimeOffset.ToString("O"));
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                if (reader.TokenType != JsonToken.String && reader.TokenType != JsonToken.Date)
                    return null;

                DateTime dt;

                var dateWithOffset = (String)reader.Value;

                if (String.IsNullOrEmpty(dateWithOffset))
                    return null;

                if (DateTime.TryParseExact(dateWithOffset, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out dt))
                    return dt;

                return null;
            }

        }

        public BsonSerializerWithUTC()
        {
            ContractResolver = new DateTimeContractResolver();
            DateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind;
        }
    }
}
