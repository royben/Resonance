using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance.Exceptions
{
    public class ResonanceHandshakeException : Exception
    {
        public ResonanceHandshakeException(String message) : base(message)
        {

        }

        public ResonanceHandshakeException(String message, Exception innerException) : base(message, innerException)
        {

        }
    }
}
