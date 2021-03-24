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
                    info.Token = reader.ReadString();
                    info.Type = (ResonanceTranscodingInformationType)reader.ReadByte();
                    info.Completed = reader.ReadBoolean();
                    info.HasError = reader.ReadBoolean();
                    info.ErrorMessage = reader.ReadString();

                    if (info.Type != ResonanceTranscodingInformationType.KeepAliveRequest && info.Type != ResonanceTranscodingInformationType.KeepAliveResponse)
                    {
                        info.Message = Decode(reader);
                    }
                }
            }
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
