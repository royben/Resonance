﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance.Reactive
{
    public interface IResonanceObservable
    {
        void OnNext(Object value);
        void OnError(Exception ex);
        void OnCompleted();
        bool IsCompleted { get; }
        bool FirstMessageArrived { get; }
        DateTime LastResponseTime { get; }
    }
}
