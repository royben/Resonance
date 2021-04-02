using Microsoft.AspNet.SignalR.Client;
using Resonance.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.SignalR.Clients
{
    internal class SignalRClient : ISignalRClient
    {
        private HubConnection _connection;
        private IHubProxy _proxy;

        public String Url { get; private set; }

        public SignalRClient(String url)
        {
            Url = url;
        }

        public Task Start()
        {
            TaskCompletionSource<object> completion = new TaskCompletionSource<object>();

            bool completed = false;

            Task.Factory.StartNew(() =>
            {
                try
                {
                    var urlHub = SplitUrl(Url);
                    _connection = new HubConnection(urlHub.url);
                    _proxy = _connection.CreateHubProxy(urlHub.hub);
                    _connection.StateChanged += (x) =>
                    {
                        if (x.NewState == ConnectionState.Connected)
                        {
                            if (!completed)
                            {
                                completed = true;
                                completion.SetResult(true);
                            }
                        }
                    };

                    _connection.Start();
                }
                catch (Exception ex)
                {
                    if (!completed)
                    {
                        completed = true;
                        completion.SetException(ex);
                    }
                }
            });

            TimeoutTask.StartNew(() =>
            {
                if (!completed)
                {
                    completed = true;
                    completion.SetException(new TimeoutException("Could not establish the connection within the given timeout."));
                }

            }, TimeSpan.FromSeconds(10));

            return completion.Task;
        }

        public Task Stop()
        {
            return Task.Factory.StartNew(() =>
            {
                _connection?.Stop();
            });
        }

        public Task Invoke(string methodName, params object[] args)
        {
            return _proxy?.Invoke(methodName, args);
        }

        public Task<T> Invoke<T>(string methodName, params object[] args)
        {
            return _proxy?.Invoke<T>(methodName, args);
        }

        public IDisposable On(string eventName, Action action)
        {
            return _proxy?.On(eventName, action);
        }

        public IDisposable On<T>(string eventName, Action<T> action)
        {
            return _proxy?.On<T>(eventName, action);
        }

        public IDisposable On<T1, T2>(string eventName, Action<T1, T2> action)
        {
            return _proxy?.On<T1, T2>(eventName, action);
        }

        public IDisposable On<T1, T2, T3>(string eventName, Action<T1, T2, T3> action)
        {
            return _proxy?.On<T1, T2, T3>(eventName, action);
        }

        private (String url, String hub) SplitUrl(String text)
        {
            var parts = text.TrimEnd('/').Split('/');

            string hub = parts.Last();
            string url = String.Join("/", parts.Take(parts.Length - 1));

            return (url, hub);
        }

        public void Dispose()
        {
            DisposeAsync().GetAwaiter().GetResult();
        }

        public async Task DisposeAsync()
        {
            await Stop();
            await Task.Factory.StartNew(() => { _connection?.Dispose(); });
        }
    }
}
