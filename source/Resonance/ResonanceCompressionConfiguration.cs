using Resonance.Compressors;
using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance
{
    /// <summary>
    /// Represents a compression configuration.
    /// </summary>
    public class ResonanceCompressionConfiguration
    {
        /// <summary>
        /// Gets or sets a value indicating whether to enable compression.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets or sets the compressor instance.
        /// </summary>
        public IResonanceCompressor Compressor { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResonanceCompressionConfiguration"/> class.
        /// </summary>
        public ResonanceCompressionConfiguration()
        {
            Compressor = new GZipCompressor();
        }
    }
}
