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
        /// <summary>
        /// Encodes the specified encoding information.
        /// </summary>
        /// <param name="info">The encoding information.</param>
        /// <returns></returns>
        public virtual byte[] Encode(ResonanceEncodingInformation info)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(ms))
                {
                    writer.Write(info.Token);
                    writer.Write((byte)info.Type);
                    writer.Write(info.Completed);
                    writer.Write(info.HasError);
                    writer.Write(info.ErrorMessage ?? String.Empty);

                    if (info.Type != ResonanceTranscodingInformationType.KeepAliveRequest && info.Type != ResonanceTranscodingInformationType.KeepAliveResponse)
                    {
                        Encode(writer, info.Message);
                    }

                    return ms.ToArray();
                }
            }
        }

        /// <summary>
        /// Encodes the specified message using the specified writer.
        /// </summary>
        /// <param name="writer">The binary writer.</param>
        /// <param name="message">The message.</param>
        protected abstract void Encode(BinaryWriter writer, Object message);

        /// <summary>
        /// Writers the header.
        /// </summary>
        /// <param name="info">The information.</param>
        /// <param name="writer">The writer.</param>
        protected virtual void WriterHeader(ResonanceEncodingInformation info, BinaryWriter writer)
        {

        }

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
