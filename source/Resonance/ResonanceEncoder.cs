using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Resonance
{
    /// <summary>
    /// Represents an <see cref="IResonanceEncoder"/> base class.
    /// </summary>
    /// <seealso cref="Resonance.IResonanceEncoder" />
    public abstract class ResonanceEncoder : IResonanceEncoder
    {
        private IResonanceHeaderTranscoder _headerTranscoder;

        /// <summary>
        /// Gets or sets the message compression configuration.
        /// </summary>
        public ResonanceCompressionConfiguration CompressionConfiguration { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResonanceEncoder"/> class.
        /// </summary>
        public ResonanceEncoder()
        {
            _headerTranscoder = OnCreateHeaderTranscoder();
            CompressionConfiguration = new ResonanceCompressionConfiguration();
        }

        /// <summary>
        /// Encodes the specified encoding information.
        /// </summary>
        /// <param name="info">The encoding information.</param>
        /// <returns></returns>
        public virtual byte[] Encode(ResonanceEncodingInformation info)
        {
            info.IsCompressed = CompressionConfiguration.Enable;

            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(ms))
                {
                    _headerTranscoder.Encode(writer, info);

                    if (info.Type != ResonanceTranscodingInformationType.KeepAliveRequest && info.Type != ResonanceTranscodingInformationType.KeepAliveResponse)
                    {
                        if (CompressionConfiguration.Enable)
                        {
                            using (MemoryStream msgMs = new MemoryStream())
                            {
                                using (BinaryWriter msgWriter = new BinaryWriter(msgMs))
                                {
                                    Encode(msgWriter, info.Message);
                                    byte[] compressedData = CompressionConfiguration.Compressor.Compress(msgMs.ToArray());
                                    writer.Write(compressedData);
                                }
                            }
                        }
                        else
                        {
                            Encode(writer, info.Message);
                        }
                    }

                    return ms.ToArray();
                }
            }
        }

        /// <summary>
        /// Override to use a different header transcoder other than the default.
        /// </summary>
        /// <returns></returns>
        protected virtual IResonanceHeaderTranscoder OnCreateHeaderTranscoder()
        {
            return new ResonanceDefaultHeaderTranscoder();
        }

        /// <summary>
        /// Encodes the specified message using the specified writer.
        /// </summary>
        /// <param name="writer">The binary writer.</param>
        /// <param name="message">The message.</param>
        protected abstract void Encode(BinaryWriter writer, Object message);

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return GetType().Name;
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        public abstract void Dispose();
    }
}
