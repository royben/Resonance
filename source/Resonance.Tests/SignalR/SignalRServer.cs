using Microsoft.AspNet.SignalR;
using Microsoft.Owin.Cors;
using Microsoft.Owin.Hosting;
using Owin;
using Resonance.SignalR;
using Resonance.SignalR.Hubs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.Tests.SignalR
{
    public class SignalRServer : IDisposable
    {
        private IDisposable _server;

        public String Url { get; set; }

        public SignalRServer()
        {
            Url = "http://localhost:8080";
        }

        public SignalRServer(String url) : this()
        {
            Url = url;
        }

        public void Start()
        {
            _server = WebApp.Start(Url);
        }

        public void Dispose()
        {
            _server?.Dispose();
        }
    }
}
