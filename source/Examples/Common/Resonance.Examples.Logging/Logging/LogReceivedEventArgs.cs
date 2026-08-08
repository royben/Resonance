using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.Examples.Common.Logging
{
    public class LogReceivedEventArgs : EventArgs
    {
        public LogEvent LogEvent { get; set; }

        public IFormatProvider FormatProvider { get; set; }
    }
}
