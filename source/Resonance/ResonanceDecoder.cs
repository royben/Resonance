using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Resonance
{
    /// <summary>
    /// Represents an <see cref="IResonanceDecoder"/> base class.
    /// </summary>
    /// <seealso cref="Resonance.IResonanceDecoder" />
    public abstract class ResonanceDecoder : IResonanceDecoder
    {
        private IResonanceHeaderTranscoder _headerTranscoder;

        /// <summary>
        /// Gets or sets the message compression configuration.
        /// </summary>
        public ResonanceCompressionConfiguration CompressionConfiguration { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResonanceDecoder"/> class.
        /// </summary>
        public ResonanceDecoder()
        {
            _headerTranscoder = OnCreateHeaderTranscoder();
            CompressionConfiguration = new ResonanceCompressionConfiguration();
        }

        /// <summary>
        /// Decodes the specified data and populates the specified decoding information.
        /// </summary>
        /// <param name="data">The encoded data.</param>
        /// <param name="info">The decoding information object to populate.</param>
        public virtual void Decode(byte[] data, ResonanceDecodingInformation info)
        {
            using (MemoryStream ms = new MemoryStream(data))
            {
                using (BinaryReader reader = new BinaryReader(ms))
                {
                    _headerTranscoder.Decode(reader, info);

                    if (info.Type != ResonanceTranscodingInformationType.KeepAliveRequest && info.Type != ResonanceTranscodingInformationType.KeepAliveResponse)
                    {
                        ms.Position = info.ActualMessageStreamPosition;

                        if (info.IsCompressed)
                        {
                            byte[] compressedData = reader.ReadBytes((int)(ms.Length - ms.Position));
                            byte[] uncompressedData = CompressionConfiguration.Compressor.Decompress(compressedData);
                            using (MemoryStream msgMs = new MemoryStream(uncompressedData))
                            {
                                using (BinaryReader msgReader = new BinaryReader(msgMs))
                                {
                                    info.Message = Decode(msgReader);
                                }
                            }
                        }
                        else
                        {
                            info.Message = Decode(reader);
                        }
                    }
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
        /// Decodes a message using the specified binary reader.
        /// </summary>
        /// <param name="reader">The binary reader.</param>
        /// <returns></returns>
        protected abstract Object Decode(BinaryReader reader);

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
