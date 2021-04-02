using Microsoft.AspNetCore.SignalR;
using Resonance.SignalR.Hubs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.Tests.SignalRCore.WebAPI.Hub
{
    public class TestHub : ResonanceHubCore<TestCredentials, TestServiceInformation, TestServiceInformation, TestAdapterInformation, TestHub, TestHubProxy>
    {
        public TestHub(IHubContext<TestHub> context) : base(context)
        {

        }
    }
}
