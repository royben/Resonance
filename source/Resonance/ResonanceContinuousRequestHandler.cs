﻿using Resonance.Reactive;
using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance
{
    internal class ResonanceContinuousRequestHandler : IResonanceRequestHandler
    {
        public ResonanceRequest Request { get; set; }
        public ResonanceContinuousRequestConfig Config { get; set; }
        public IResonanceObservable ContinuousObservable { get; set; }
        public TimeSpan? ContinuousTimeout { get; set; }
    }
}
