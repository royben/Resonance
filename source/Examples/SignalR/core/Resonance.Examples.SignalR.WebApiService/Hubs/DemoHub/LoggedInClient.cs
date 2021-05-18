using Resonance.Examples.SignalR.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Resonance.Examples.SignalR.WebApiService.Hubs.DemoHub
{
    public class LoggedInClient
    {
        public String ConnectionId { get; set; }

        public DemoCredentials Credentials { get; set; }

        public DemoAdapterInformation AdapterInformation { get; set; }
    }
}
