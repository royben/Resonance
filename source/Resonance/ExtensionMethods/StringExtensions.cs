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
    }
}
