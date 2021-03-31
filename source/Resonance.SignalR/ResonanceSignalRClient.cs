using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance.SignalR
{
    public class ResonanceSignalRClient : IResonanceHub
    {
        private HubConnection _connection;

        public String Url { get; set; }

        public ResonanceSignalRClient(String url)
        {
            Url = url;
        }

        public void Start()
        {
            _connection = new HubConnectionBuilder()
              .WithUrl(Url)
              .Build();

            _connection.StartAsync().GetAwaiter().GetResult();
        }

        public string GetString(string input)
        {
            String output = _connection.InvokeAsync<String>("GetString", input).GetAwaiter().GetResult();
            return output;
        }
    }
}
