using Resonance.Reactive;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Resonance
{
    internal class ResonanceRequestHandler : IResonanceRequestHandler
    {
        public ResonanceRequest Request { get; set; }
        public ResonanceRequestConfig Config { get; set; }
        public TaskCompletionSource<Object> CompletionSource { get; set; }
    }
}
