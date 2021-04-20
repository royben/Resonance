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

        /// <summary>
        /// Chunk this array to segments.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="chunkSize">Size of the chunk.</param>
        /// <returns></returns>
        public static List<byte[]> ToChunks(this byte[] data, int chunkSize)
        {
            List<byte[]> segments = new List<byte[]>();
            int index = 0;

            while (index < data.Length)
            {
                int count = Math.Min(chunkSize, data.Length - index);
                byte[] chunk = new byte[count];
                Buffer.BlockCopy(data, index, chunk, 0, count);
                segments.Add(chunk);
                index += count;
            }

            return segments;
        }

        public static byte[] TakeFrom(this byte[] data, int index)
        {
            byte[] chunk = new byte[data.Length - index];
            Buffer.BlockCopy(data, index, chunk, 0, chunk.Length);
            return chunk;
        }
    }
}
