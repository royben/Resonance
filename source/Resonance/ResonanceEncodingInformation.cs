using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance
{
    public class ResonanceEncodingInformation
    {
        public bool IsRequest { get; set; }
        public String Token { get; set; }
        public bool Completed { get; set; }
        public bool HasError { get; set; }
        public String ErrorMessage { get; set; }
        public object Message { get; set; }
    }
}
