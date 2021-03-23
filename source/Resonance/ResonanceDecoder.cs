using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Resonance
{
    public abstract class ResonanceDecoder : IResonanceDecoder
    {
        public abstract void Decode(byte[] data, ResonanceDecodingInformation info);
        public abstract void Dispose();

        protected virtual void ReadHeader(ResonanceDecodingInformation info, BinaryReader reader)
        {
            info.Token = reader.ReadString();
            info.IsRequest = reader.ReadBoolean();
            info.Completed = reader.ReadBoolean();
            info.HasError = reader.ReadBoolean();
            info.ErrorMessage = reader.ReadString();
        }

        public override string ToString()
        {
            return GetType().Name;
        }
    }
}
