using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance.Tokens
{
    public class GuidTokenGenerator : IResonanceTokenGenerator
    {
        public string Generate(object message)
        {
            return Guid.NewGuid().ToString();
        }
    }
}
