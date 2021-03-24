using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.Tests.Common.Transcoding
{
    public class CorruptedDecoder : ResonanceDecoder
    {
        public override void Dispose()
        {
            //throw new NotImplementedException();
        }

        protected override object Decode(BinaryReader reader)
        {
            throw new CorruptedDecoderException();
        }
    }
}
