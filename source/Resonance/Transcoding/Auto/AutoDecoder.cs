using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Resonance.Transcoding.Auto
{
    /// <summary>
    /// Represents an automatic decoder capable of decoding messages based on the transcoding header information.
    /// </summary>
    /// <seealso cref="Resonance.ResonanceDecoder" />
    public class AutoDecoder : ResonanceDecoder
    {
        private IResonanceDecoder _decoder;

        /// <summary>
        /// Called when the transcoding information is first available.
        /// </summary>
        /// <param name="info">The transcoding information.</param>
        protected override void OnTranscodingInformationDecoded(ResonanceDecodingInformation info)
        {
            base.OnTranscodingInformationDecoded(info);
            _decoder = ResonanceTranscodingFactory.Default.GetDecoder(info.Transcoding);
        }

        /// <summary>
        /// Decodes a message from the specified memory stream.
        /// </summary>
        /// <param name="stream">The memory stream.</param>
        /// <returns></returns>
        public override object Decode(MemoryStream stream)
        {
            return _decoder.Decode(stream);
        }
    }
}
