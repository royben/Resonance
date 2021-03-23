﻿using Resonance.Reactive;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Resonance
{
    internal class ResonancePendingContinuousRequest : IResonancePendingRequest
    {
        public ResonanceRequest Request { get; set; }
        public ResonanceContinuousRequestConfig Config { get; set; }
        public IResonanceObservable ContinuousObservable { get; set; }
        public TimeSpan? ContinuousTimeout { get; set; }
        public CancellationToken CancellationToken { get; set; }
    }
}
