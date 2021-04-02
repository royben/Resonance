using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.SignalR.Clients
{
    internal class SignalRCoreClient : ISignalRClient
    {
        private HubConnection _connection;

        public String Url { get; }

        public SignalRCoreClient(String url)
        {
            Url = url;
        }

        public Task Start()
        {
            _connection = new HubConnectionBuilder().WithUrl(Url).Build();
            return _connection.StartAsync();
        }

        public Task Stop()
        {
            return _connection.StopAsync();
        }

        public Task Invoke(string methodName, params object[] args)
        {
            if (args == null || args.Length == 0)
            {
                return _connection?.InvokeAsync(methodName);
            }
            else if (args.Length == 1)
            {
                return _connection?.InvokeAsync(methodName, args[0]);
            }
            else if (args.Length == 2)
            {
                return _connection?.InvokeAsync(methodName, args[1]);
            }
            else if (args.Length == 3)
            {
                return _connection?.InvokeAsync(methodName, args[2]);
            }

            throw new ArgumentOutOfRangeException("This method does not support more than 3 arguments.");
        }

        public Task<T> Invoke<T>(string methodName, params object[] args)
        {
            if (args == null || args.Length == 0)
            {
                return _connection?.InvokeAsync<T>(methodName);
            }
            else if (args.Length == 1)
            {
                return _connection?.InvokeAsync<T>(methodName, args[0]);
            }
            else if (args.Length == 2)
            {
                return _connection?.InvokeAsync<T>(methodName, args[1]);
            }
            else if (args.Length == 3)
            {
                return _connection?.InvokeAsync<T>(methodName, args[2]);
            }

            throw new ArgumentOutOfRangeException("This method does not support more than 3 arguments.");
        }

        public IDisposable On(string eventName, Action action)
        {
            return _connection?.On(eventName, action);
        }

        public IDisposable On<T>(string eventName, Action<T> action)
        {
            return _connection?.On<T>(eventName, action);
        }

        public IDisposable On<T1, T2>(string eventName, Action<T1, T2> action)
        {
            return _connection?.On<T1, T2>(eventName, action);
        }

        public IDisposable On<T1, T2, T3>(string eventName, Action<T1, T2, T3> action)
        {
            return _connection?.On<T1, T2, T3>(eventName, action);
        }

        public void Dispose()
        {
            DisposeAsync().GetAwaiter().GetResult();
        }

        public async Task DisposeAsync()
        {
            await Stop();
            await _connection.DisposeAsync();
        }
    }
}
