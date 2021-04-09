using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance.ExtensionMethods
{
    public static class ByteArrayExtensions
    {
        /// <summary>
        /// Gets the byte array length in a human readable format.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        public static String ToFriendlyByteSize(this byte[] data)
        {
            var size = data.Length;

            string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" }; //Longs run out around EB
            if (size == 0)
                return "0" + suf[0];
            long bytes = Math.Abs(size);
            int place = System.Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            double num = Math.Round(bytes / Math.Pow(1024, place), 1);
            return (Math.Sign(size) * num).ToString() + " " + suf[place];
        }
    }
}
