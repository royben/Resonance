using Microsoft.AspNet.SignalR.Client;
using Resonance.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.SignalR.Clients
{
    /// <summary>
    /// Represents a SignalR legacy client wrapper.
    /// </summary>
    /// <seealso cref="Resonance.SignalR.Clients.ISignalRClient" />
    public class SignalRClient : ISignalRClient
    {
        private HubConnection _connection;
        private IHubProxy _proxy;

        /// <summary>
        /// Gets the hub URL, meaning, service url + /hub.
        /// </summary>
        public String Url { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this client has started.
        /// </summary>
        public bool IsStarted { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SignalRClient"/> class.
        /// </summary>
        /// <param name="url">the hub URL, meaning, service url + /hub.</param>
        public SignalRClient(String url)
        {
            Url = url;
        }

        /// <summary>
        /// Starts the connection.
        /// </summary>
        /// <returns></returns>
        public Task Start()
        {
            if (IsStarted) return Task.FromResult(true);

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
                                IsStarted = true;
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

        /// <summary>
        /// Stops the connection.
        /// </summary>
        /// <returns></returns>
        public Task Stop()
        {
            if (!IsStarted) return Task.FromResult(true);

            return Task.Factory.StartNew(() =>
            {
                IsStarted = false;
                _connection?.Stop();
            });
        }

        /// <summary>
        /// Invokes the specified hub method without expecting a return value.
        /// </summary>
        /// <param name="methodName">Name of the method.</param>
        /// <param name="args">The arguments.</param>
        /// <returns></returns>
        public Task Invoke(string methodName, params object[] args)
        {
            return _proxy?.Invoke(methodName, args);
        }

        /// <summary>
        /// Invokes the specified hub method and return a value of type T.
        /// </summary>
        /// <typeparam name="T">Type of return value</typeparam>
        /// <param name="methodName">Name of the method.</param>
        /// <param name="args">The arguments.</param>
        /// <returns></returns>
        public Task<T> Invoke<T>(string methodName, params object[] args)
        {
            return _proxy?.Invoke<T>(methodName, args);
        }

        /// <summary>
        /// Register a callback method for a hub event.
        /// </summary>
        /// <param name="eventName">Name of the event.</param>
        /// <param name="action">The callback.</param>
        /// <returns></returns>
        public IDisposable On(string eventName, Action action)
        {
            return _proxy?.On(eventName, action);
        }

        /// <summary>
        /// Register a callback method for a hub event.
        /// </summary>
        /// <typeparam name="T">Type of expected callback parameter.</typeparam>
        /// <param name="eventName">Name of the event.</param>
        /// <param name="action">The callback.</param>
        /// <returns></returns>
        public IDisposable On<T>(string eventName, Action<T> action)
        {
            return _proxy?.On<T>(eventName, action);
        }

        /// <summary>
        /// Register a callback method for a hub event.
        /// </summary>
        /// <typeparam name="T1">Type of first expected callback parameter.</typeparam>
        /// <typeparam name="T2">Type of second expected callback parameter.</typeparam>
        /// <param name="eventName">Name of the event.</param>
        /// <param name="action">The callback.</param>
        /// <returns></returns>
        public IDisposable On<T1, T2>(string eventName, Action<T1, T2> action)
        {
            return _proxy?.On<T1, T2>(eventName, action);
        }

        /// <summary>
        /// Register a callback method for a hub event.
        /// </summary>
        /// <typeparam name="T1">Type of first expected callback parameter.</typeparam>
        /// <typeparam name="T2">Type of second expected callback parameter.</typeparam>
        /// <typeparam name="T3">Type of third expected callback parameter.</typeparam>
        /// <param name="eventName">Name of the event.</param>
        /// <param name="action">The callback.</param>
        /// <returns></returns>
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

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            DisposeAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Closes the connection.
        /// </summary>
        public async Task DisposeAsync()
        {
            await Stop();
            await Task.Factory.StartNew(() => { _connection?.Dispose(); });
        }
    }
}
