using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Resonance
{
    public abstract class ResonanceEncoder : IResonanceEncoder
    {
        public abstract void Dispose();
        public abstract byte[] Encode(ResonanceEncodingInformation message);

        protected virtual void WriterHeader(ResonanceEncodingInformation info, BinaryWriter writer)
        {
            writer.Write(info.Token);
            writer.Write(info.IsRequest);
            writer.Write(info.Completed);
            writer.Write(info.HasError);
            writer.Write(info.ErrorMessage ?? String.Empty);
        }

        public override string ToString()
        {
            return GetType().Name;
        }
    }
}
