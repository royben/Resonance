using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace Resonance
{
    /// <summary>
    /// Represents an <see cref="IResonanceEncoder"/> base class.
    /// </summary>
    /// <seealso cref="Resonance.IResonanceEncoder" />
    public abstract class ResonanceEncoder : IResonanceEncoder
    {
        private readonly IResonanceHeaderTranscoder _headerTranscoder;
        private readonly String _transcodingName;

        /// <summary>
        /// Gets or sets the message compression configuration.
        /// </summary>
        public ResonanceCompressionConfiguration CompressionConfiguration { get; }

        /// <summary>
        /// Gets the encryption configuration.
        /// </summary>
        public ResonanceEncryptionConfiguration EncryptionConfiguration { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResonanceEncoder"/> class.
        /// </summary>
        public ResonanceEncoder()
        {
            var att = this.GetType().GetCustomAttribute<ResonanceTranscodingAttribute>();
            _transcodingName = att?.Name;
            _headerTranscoder = OnCreateHeaderTranscoder();
            CompressionConfiguration = ResonanceGlobalSettings.Default.DefaultCompressionConfiguration.Clone();
            EncryptionConfiguration = ResonanceGlobalSettings.Default.DefaultEncryptionConfiguration.Clone();
        }

        /// <summary>
        /// Encodes the specified encoding information.
        /// </summary>
        /// <param name="info">The encoding information.</param>
        /// <returns></returns>
        public virtual byte[] Encode(ResonanceEncodingInformation info)
        {
            info.IsCompressed = CompressionConfiguration.Enabled;
            info.IsEncrypted = EncryptionConfiguration.Enabled;
            info.Transcoding = _transcodingName;

            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(ms))
                {
                    _headerTranscoder.Encode(writer, info);

                    if (info.Type != ResonanceTranscodingInformationType.KeepAliveRequest && info.Type != ResonanceTranscodingInformationType.KeepAliveResponse)
                    {
                        byte[] msgData = null;

                        using (MemoryStream msgMs = new MemoryStream())
                        {
                            using (BinaryWriter msgWriter = new BinaryWriter(msgMs))
                            {
                                Encode(msgWriter, info.Message);
                                msgData = msgMs.ToArray();
                            }
                        }

                        if (EncryptionConfiguration.Enabled)
                        {
                            msgData = EncryptMessageData(msgData);
                        }

                        if (CompressionConfiguration.Enabled)
                        {
                            msgData = CompressMessageData(msgData);
                        }

                        writer.Write(msgData);
                    }

                    return ms.ToArray();
                }
            }
        }

        /// <summary>
        /// Encrypts the message data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        protected virtual byte[] EncryptMessageData(byte[] data)
        {
            using (MemoryStream encryptedMs = new MemoryStream())
            {
                CryptoStream cs = new CryptoStream(encryptedMs, EncryptionConfiguration.SymmetricAlgorithm.CreateEncryptor(), CryptoStreamMode.Write);
                cs.Write(data, 0, data.Length);
                cs.Close();
                return encryptedMs.ToArray();
            }
        }

        /// <summary>
        /// Compresses the message data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        protected virtual byte[] CompressMessageData(byte[] data)
        {
            return CompressionConfiguration.Compressor.Compress(data);
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
            return this != null ? GetType().Name : "No Encoder";
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        public abstract void Dispose();
    }
}
