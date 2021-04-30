using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.Examples.Common.Logging
{
    public class LogEventVM : ResonanceViewModel
    {
        public String SourceContext { get; set; }

        public String Time { get; set; }

        public LogEventLevel Level { get; set; }

        public String Message { get; set; }

        public String Exception { get; set; }

        public String Token { get; set; }

        public LogEventVM(LogEvent logEvent, IFormatProvider formatProvider)
        {
            Level = logEvent.Level;
            Time = logEvent.Timestamp.ToString("HH:mm:ss.fff");
            Message = logEvent.RenderMessage(formatProvider);
            Exception = logEvent.Exception?.Message;

            LogEventPropertyValue contextValue = null;

            if (logEvent.Properties.TryGetValue("SourceContext", out contextValue))
            {
                SourceContext = contextValue?.ToString().Replace("\"", "");
            }

            LogEventPropertyValue tokenValue = null;

            if (logEvent.Properties.TryGetValue("Token", out tokenValue))
            {
                Token = tokenValue?.ToString();
            }
        }
    }
}
