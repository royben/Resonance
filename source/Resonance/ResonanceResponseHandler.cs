using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Resonance
{
    internal class ResonanceResponseHandler
    {
        public ResonanceResponse Response { get; set; }
        public TaskCompletionSource<Object> CompletionSource { get; set; }
        public ResonanceResponseConfig Config { get; set; }
    }
}
