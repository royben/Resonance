using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Resonance.Compressors
{
    /// <summary>
    /// Represents a GZip Resonance compressor.
    /// </summary>
    /// <seealso cref="Resonance.IResonanceCompressor" />
    public class GZipCompressor : IResonanceCompressor
    {
        /// <summary>
        /// Compresses the specified data and returns the compressed data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        public byte[] Compress(byte[] data)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (GZipStream zip = new GZipStream(ms, CompressionMode.Compress, true))
                {
                    zip.Write(data, 0, data.Length);
                }

                return ms.ToArray();
            }
        }

        /// <summary>
        /// Decompresses the specified compressed data.
        /// </summary>
        /// <param name="compressedData">The compressed data.</param>
        /// <returns></returns>
        public byte[] Decompress(byte[] compressedData)
        {
            using (MemoryStream msOut = new MemoryStream())
            {
                using (MemoryStream ms = new MemoryStream(compressedData))
                {
                    using (GZipStream zip = new GZipStream(ms, CompressionMode.Decompress))
                    {
                        zip.CopyTo(msOut);
                    }
                }
                return msOut.ToArray();
            }
        }
    }
}
