using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Resonance
{
    /// <summary>
    /// Represents an <see cref="IResonanceDecoder"/> base class.
    /// </summary>
    /// <seealso cref="Resonance.IResonanceDecoder" />
    public abstract class ResonanceDecoder : IResonanceDecoder
    {
        private readonly IResonanceHeaderTranscoder _headerTranscoder;

        /// <summary>
        /// Gets or sets the message compression configuration.
        /// </summary>
        public ResonanceCompressionConfiguration CompressionConfiguration { get; }

        /// <summary>
        /// Gets the encryption configuration.
        /// </summary>
        public ResonanceEncryptionConfiguration EncryptionConfiguration { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResonanceDecoder"/> class.
        /// </summary>
        public ResonanceDecoder()
        {
            _headerTranscoder = OnCreateHeaderTranscoder();
            CompressionConfiguration = ResonanceGlobalSettings.Default.DefaultCompressionConfiguration.Clone();
            EncryptionConfiguration = ResonanceGlobalSettings.Default.DefaultEncryptionConfiguration.Clone();
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
                    OnTranscodingInformationDecoded(info);

                    if (info.Type != ResonanceTranscodingInformationType.KeepAliveRequest && info.Type != ResonanceTranscodingInformationType.KeepAliveResponse)
                    {
                        ms.Position = info.ActualMessageStreamPosition;

                        byte[] msgData = reader.ReadBytes((int)(ms.Length - ms.Position));

                        if (info.IsCompressed)
                        {
                            msgData = DecompressMessageData(msgData);
                        }

                        if (EncryptionConfiguration.Enabled)
                        {
                            msgData = DecryptMessageData(msgData);
                        }

                        using (MemoryStream msgMs = new MemoryStream(msgData))
                        {
                            info.Message = Decode(msgMs);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Called when the transcoding information is first available.
        /// </summary>
        /// <param name="info">The transcoding information.</param>
        protected virtual void OnTranscodingInformationDecoded(ResonanceDecodingInformation info)
        {
            //Leave it.
        }

        /// <summary>
        /// Decodes the specified data and returns the <see cref="ResonanceTranscodingInformation.Message"/> as type T.
        /// </summary>
        /// <typeparam name="T">Type of expected message.</typeparam>
        /// <param name="data">The encoded data.</param>
        /// <returns></returns>
        public T Decode<T>(byte[] data)
        {
            ResonanceDecodingInformation info = new ResonanceDecodingInformation();
            Decode(data, info);
            return (T)info.Message;
        }

        /// <summary>
        /// Decompresses the message data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        protected virtual byte[] DecompressMessageData(byte[] data)
        {
            return CompressionConfiguration.Compressor.Decompress(data);
        }

        /// <summary>
        /// Decrypts the message data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        protected virtual byte[] DecryptMessageData(byte[] data)
        {
            using (MemoryStream decryptedMs = new MemoryStream())
            {
                CryptoStream cs = new CryptoStream(decryptedMs, EncryptionConfiguration.SymmetricAlgorithm.CreateDecryptor(), CryptoStreamMode.Write);
                cs.Write(data, 0, data.Length);
                cs.Close();
                return decryptedMs.ToArray();
            }
        }

        /// <summary>
        /// Override to use a different header transcoder other than the default.
        /// </summary>
        /// <returns></returns>
        protected virtual IResonanceHeaderTranscoder OnCreateHeaderTranscoder()
        {
            return ResonanceGlobalSettings.Default.DefaultHeaderTranscoder;
        }

        /// <summary>
        /// Decodes a message from the specified memory stream.
        /// </summary>
        /// <param name="stream">The memory stream.</param>
        public abstract Object Decode(MemoryStream stream);

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return this != null ? GetType().Name : "No Decoder";
        }
    }
}
