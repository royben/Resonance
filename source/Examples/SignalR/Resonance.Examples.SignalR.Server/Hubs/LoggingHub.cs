using Microsoft.AspNetCore.SignalR;
using Resonance.Examples.Common.Logging;
using System;

namespace Resonance.Examples.SignalR.Server.Hubs
{
    /// <summary>
    /// Broadcasts Resonance log events to the demo web page.
    /// </summary>
    public class LoggingHub : Hub
    {
        private static IHubContext<LoggingHub> _context;

        /// <summary>
        /// Wires the static Serilog sink event to this hub.
        /// ASP.NET Core resolves IHubContext from DI rather than a legacy GlobalHost singleton.
        /// </summary>
        internal static void Attach(IHubContext<LoggingHub> context)
        {
            _context = context;
            LoggingConfiguration.LogReceived += (_, e) => PublishLog(e);
        }

        internal static void PublishLog(LogReceivedEventArgs e)
        {
            _context?.Clients.All.SendAsync("LogReceived", new LogEventVM(e.LogEvent, e.FormatProvider));
        }
    }
}
