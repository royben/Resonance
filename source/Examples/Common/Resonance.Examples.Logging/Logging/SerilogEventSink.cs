using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Display;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.Examples.Common.Logging
{
    public class SerilogEventSink : ILogEventSink
    {
        private readonly IFormatProvider _formatProvider;

        public SerilogEventSink(IFormatProvider formatProvider)
        {
            _formatProvider = formatProvider;
        }

        public void Emit(LogEvent logEvent)
        {
            LoggingConfiguration.RaiseLogEvent(logEvent, _formatProvider);
        }
    }

    public static class SerilogExtensions
    {
        public static LoggerConfiguration SerilogEventSink(
            this LoggerSinkConfiguration loggerConfiguration,
            IFormatProvider formatProvider = null)
        {
            return loggerConfiguration.Sink(new SerilogEventSink(formatProvider));
        }
    }
}
