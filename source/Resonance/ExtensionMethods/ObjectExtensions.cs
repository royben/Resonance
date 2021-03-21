using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance.ExtensionMethods
{
    internal static class ObjectExtensions
    {
        private static JsonSerializerSettings _jsonSettings = new JsonSerializerSettings();

        static ObjectExtensions()
        {
            var _jsonSettings = new JsonSerializerSettings();
            _jsonSettings.Converters.Add(new StringEnumConverter { NamingStrategy = new CamelCaseNamingStrategy() });
        }

        /// <summary>
        /// Serializes the specified object to indented json string.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <returns></returns>
        public static String ToJsonString(this Object obj)
        {
            if (obj == null) return "null";
            return JsonConvert.SerializeObject(obj, Formatting.Indented, _jsonSettings);
        }
    }
}
