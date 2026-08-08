using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.Examples.Common.Logging
{
    public static class LoggingConfiguration
    {
        public static event EventHandler<LogReceivedEventArgs> LogReceived;

        public static LoggingLevelSwitch LoggingLevelSwitch { get; set; }

        public static void RaiseLogEvent(LogEvent logEvent,IFormatProvider formatProvider)
        {
            LogReceived?.Invoke(null, new LogReceivedEventArgs() { LogEvent = logEvent, FormatProvider = formatProvider });
        }

        public static void ConfigureLogging()
        {
            var loggerFactory = new LoggerFactory();

            LoggingLevelSwitch = new LoggingLevelSwitch(LogEventLevel.Information);

            var logger = new LoggerConfiguration()
                .MinimumLevel
                .ControlledBy(LoggingLevelSwitch)
                .WriteTo
                .SerilogEventSink()
                .CreateLogger();

            loggerFactory.AddSerilog(logger);

            ResonanceGlobalSettings.Default.LoggerFactory = loggerFactory;
        }
    }
}
