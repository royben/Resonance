using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance
{
    internal class ResonanceRequestHandler
    {
        public bool HasResponse { get; set; }
        public Type RequestType { get; set; }
        public Action<IResonanceTransporter, Object> Callback { get; set; }
        public Func<Object, Object> ResponseCallback { get; set; }
        public object RegisteredCallback { get; set; }
        public IResonanceService Service { get; set; }
        public String RegisteredCallbackDescription { get; set; }
    }
}
