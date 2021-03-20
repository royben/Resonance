using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.Core
{
    public class ResonanceRequestFailedEventArgs : EventArgs
    {
        public ResonanceRequest Request { get; set; }

        public Exception Exception { get; set; }

        public ResonanceRequestFailedEventArgs(ResonanceRequest request, Exception exception)
        {
            Request = request;
            Exception = exception;
        }
    }
}
