using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance.ExtensionMethods
{
    public static class StringExtensions
    {
        /// <summary>
        /// Returns null if this string is empty or null.
        /// </summary>
        /// <param name="str">The string.</param>
        public static String ToNullIfEmpty(this String str)
        {
            if (str == null) return null;
            if (String.IsNullOrEmpty(str)) return null;
            return str;
        }

        /// <summary>
        /// Returns true if this string is not null or empty.
        /// </summary>
        /// <param name="str">The string.</param>
        public static bool IsNotNullOrEmpty(this String str)
        {
            return !String.IsNullOrWhiteSpace(str);
        }
        /// <summary>
        /// Returns true if this string is null or empty.
        /// </summary>
        /// <param name="str">The string.</param>
        public static bool IsNullOrEmpty(this String str)
        {
            return String.IsNullOrWhiteSpace(str);
        }

        /// <summary>
        /// Truncates the specified string to the specified max length and appends ellipsis.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="maxLength">Max length</param>
        /// <returns></returns>
        public static String Ellipsis(this String text, int maxLength)
        {
            return text.Length <= maxLength ? text : text.Substring(0, maxLength) + "...";
        }
    }
}
