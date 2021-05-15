using Microsoft.AspNet.SignalR;
using Resonance.Examples.Common.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace Resonance.Examples.SignalR.Server.Hubs
{
    public class LoggingHub : Hub
    {
        private static IHubContext hubContext = GlobalHost.ConnectionManager.GetHubContext<LoggingHub>();

        internal static void PublishLog(LogReceivedEventArgs e)
        {
            hubContext.Clients.All.LogReceived(new LogEventVM(e.LogEvent, e.FormatProvider));
        }
    }
}