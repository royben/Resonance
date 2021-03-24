using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance
{
    /// <summary>
    /// Represents a data compression/decompression interface.
    /// </summary>
    public interface IResonanceCompressor
    {
        /// <summary>
        /// Compresses the specified data and returns the compressed data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        byte[] Compress(byte[] data);

        /// <summary>
        /// Decompresses the specified compressed data.
        /// </summary>
        /// <param name="compressedData">The compressed data.</param>
        /// <returns></returns>
        byte[] Decompress(byte[] compressedData);
    }
}
