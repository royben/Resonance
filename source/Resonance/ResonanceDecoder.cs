using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Resonance
{
    public abstract class ResonanceDecoder : IResonanceDecoder
    {
        public abstract ResonanceTranscodingInformation Decode(byte[] data);
        public abstract void Dispose();

        protected virtual void ReadHeader(ResonanceTranscodingInformation info, BinaryReader reader)
        {
            info.Token = reader.ReadString();
            info.IsRequest = reader.ReadBoolean();
            info.Completed = reader.ReadBoolean();
            info.HasError = reader.ReadBoolean();
            info.ErrorMessage = reader.ReadString();
        }
    }
}
