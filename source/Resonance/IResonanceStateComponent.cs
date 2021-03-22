using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance
{
    public interface IResonanceStateComponent
    {
        event EventHandler<ResonanceComponentState> StateChanged;
        ResonanceComponentState State { get; }
        Exception FailedStateException { get; }
    }
}
