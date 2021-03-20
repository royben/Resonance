using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance.Core
{
    public class ResonanceResponse : IResonanceMessage
    {
        public String Token { get; set; }
        public object Message { get; set; }
    }

    public class ResonanceResponse<T> : ResonanceResponse
    {
        public new T Message
        {
            get { return (T)base.Message; }
        }
    }
}
