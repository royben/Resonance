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
        public override void Decode(byte[] data, ResonanceDecodingInformation info)
        {
            using (MemoryStream ms = new MemoryStream(data))
            {
                using (BinaryReader reader = new BinaryReader(ms))
                {
                    ReadHeader(info, reader);
                    throw new CorruptedDecoderException();
                }
            }
        }

        public override void Dispose()
        {
            //throw new NotImplementedException();
        }
    }
}
