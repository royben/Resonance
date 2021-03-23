using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance
{
    public class ResonanceDecodingInformation : ResonanceEncodingInformation
    {
        public Exception DecoderException { get; set; }

        public bool HasDecodingException
        {
            get { return DecoderException != null; }
        }

    }
}
