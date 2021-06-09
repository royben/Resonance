﻿using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance.ExtensionMethods
{
    public static class ObjectExtensions
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

        /// <summary>
        /// Returns the object's ToString() value. If the object is null, will return and String.Empty.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <returns></returns>
        public static String ToStringOrEmpty(this object obj)
        {
            return obj != null ? obj.ToString() : String.Empty;
        }

        /// <summary>
        /// Gets the name of this object type. If the object is null, will return "null";
        /// </summary>
        /// <param name="obj">The object.</param>
        public static String GetTypeName(this Object obj)
        {
            if (obj == null) return "null";
            return obj.GetType().Name;
        }
    }
}
