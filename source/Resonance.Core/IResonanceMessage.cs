using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance.Core
{
    public interface IResonanceMessage
    {
        String Token { get; set; }
        Object Message { get; set; }
    }
}
