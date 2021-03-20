using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance.Core
{
    public class ResonanceTranscodingInformation
    {
        public String Token { get; set; }
        public bool IsContinuous { get; set; }
        public bool Completed { get; set; }
        public bool HasError { get; set; }
        public int? ErrorCode { get; set; }
        public String ErrorMessage { get; set; }
        public object Message { get; set; }
    }
}
