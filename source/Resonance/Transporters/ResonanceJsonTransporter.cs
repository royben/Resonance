using Resonance.Transcoding.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance.Transporters
{
    public class ResonanceJsonTransporter : ResonanceTransporter
    {
        public ResonanceJsonTransporter() : base()
        {
            Encoder = new JsonEncoder();
            Decoder = new JsonDecoder();
        }

        public ResonanceJsonTransporter(IResonanceAdapter adapter) : this()
        {
            Adapter = adapter;
        }
    }
}
