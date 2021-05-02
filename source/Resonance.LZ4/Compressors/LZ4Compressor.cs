using K4os.Compression.LZ4;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Resonance.Compressors
{
    /// <summary>
    /// Represents an LZ4 Resonance fast compression engine.
    /// </summary>
    /// <seealso cref="Resonance.IResonanceCompressor" />
    public class LZ4Compressor : IResonanceCompressor
    {
        /// <summary>
        /// Compresses the specified data and returns the compressed data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        public byte[] Compress(byte[] data)
        {
            return LZ4Pickler.Pickle(data);
        }

        /// <summary>
        /// Decompresses the specified compressed data.
        /// </summary>
        /// <param name="compressedData">The compressed data.</param>
        /// <returns></returns>
        public byte[] Decompress(byte[] compressedData)
        {
            return LZ4Pickler.Unpickle(compressedData);
        }
    }
}
