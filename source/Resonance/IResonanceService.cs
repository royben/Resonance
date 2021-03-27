using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance
{
    public interface IResonanceService
    {
        void OnTransporterStateChanged(ResonanceComponentState state);
    }
}
