using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance
{
    /// <summary>
    /// Represents a Resonance incoming message decoding information.
    /// </summary>
    /// <seealso cref="Resonance.ResonanceTranscodingInformation" />
    public class ResonanceDecodingInformation : ResonanceTranscodingInformation
    {
        /// <summary>
        /// Gets or sets the protocol version.
        /// </summary>
        public int ProtocolVersion { get; set; }

        /// <summary>
        /// Gets or sets the actual message stream position.
        /// </summary>
        public uint ActualMessageStreamPosition { get; set; }

        /// <summary>
        /// Gets or sets an optional decoder exception that has occurred during the decoding.
        /// </summary>
        public Exception DecoderException { get; set; }

        /// <summary>
        /// Gets a value indicating whether a decoding exception has occurred while decoding.
        /// </summary>
        public bool HasDecodingException
        {
            get { return DecoderException != null; }
        }
    }
}
