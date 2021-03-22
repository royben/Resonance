using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance.Exceptions
{
    public class ResonanceTransporterDisconnectedException : Exception
    {
        public ResonanceTransporterDisconnectedException(String message) : base(message)
        {

        }
    }
}
