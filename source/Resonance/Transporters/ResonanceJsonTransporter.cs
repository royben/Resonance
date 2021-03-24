using Resonance.Transcoding.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance.Transporters
{
    /// <summary>
    /// Represents a Json transporter with <see cref="JsonEncoder"/> and <see cref="JsonDecoder"/> as the transcoding.
    /// </summary>
    /// <seealso cref="Resonance.ResonanceTransporter" />
    public class ResonanceJsonTransporter : ResonanceTransporter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ResonanceJsonTransporter"/> class.
        /// </summary>
        public ResonanceJsonTransporter() : base()
        {
            Encoder = new JsonEncoder();
            Decoder = new JsonDecoder();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResonanceJsonTransporter"/> class.
        /// </summary>
        /// <param name="adapter">The adapter.</param>
        public ResonanceJsonTransporter(IResonanceAdapter adapter) : this()
        {
            Adapter = adapter;
        }
    }
}
