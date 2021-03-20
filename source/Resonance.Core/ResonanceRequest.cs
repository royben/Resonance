using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance.Core
{
    public class ResonanceRequest : IResonanceMessage
    {
        public String Token { get; set; }
        public object Message { get; set; }
    }

    public class ResonanceRequest<T> : ResonanceRequest
    {
        public new T Message
        {
            get { return (T)base.Message; }
        }
    }
}
