using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance
{
    internal interface IResonancePendingRequest
    {
        ResonanceRequest Request { get; set; }
    }
}
