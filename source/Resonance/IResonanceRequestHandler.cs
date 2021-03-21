using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance
{
    internal interface IResonanceRequestHandler
    {
        ResonanceRequest Request { get; set; }
    }
}
