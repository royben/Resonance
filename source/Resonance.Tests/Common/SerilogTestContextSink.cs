using Microsoft.VisualStudio.TestTools.UnitTesting;
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

namespace Resonance.Tests.Common
{
    public class SerilogTestContextSink : ILogEventSink
    {
        const string DefaultDebugOutputTemplate = "[{SourceContext}] [{Level}] [{Timestamp:HH:mm:ss.fff}]: {Message}{NewLine}{Exception}";

        private TestContext _context;
        private readonly ITextFormatter _formatter;

        public SerilogTestContextSink(TestContext context)
        {
            _context = context;
            _formatter = new MessageTemplateTextFormatter(DefaultDebugOutputTemplate, null);
        }

        public void Emit(LogEvent logEvent)
        {
            using (var buffer = new StringWriter())
            {
                _formatter.Format(logEvent, buffer);
                _context.WriteLine(buffer.ToString().Trim());
            }
        }
    }
}
