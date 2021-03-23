using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance
{
    public interface IResonanceTokenGenerator
    {
        String GenerateToken(Object message);
    }
}
