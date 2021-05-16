using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.SignalR.Clients
{
    /// <summary>
    /// Represents a SignalR Core client wrapper.
    /// </summary>
    /// <seealso cref="Resonance.SignalR.Clients.ISignalRClient" />
    internal class SignalRCoreClient : ISignalRClient
    {
        private HubConnection _connection;

        /// <summary>
        /// Occurs when an error has occurred on the client.
        /// </summary>
        public event EventHandler<ResonanceExceptionEventArgs> Error;

        /// <summary>
        /// Occurs when the client is trying to reconnect after a connection loss.
        /// </summary>
        public event EventHandler Reconnecting;

        /// <summary>
        /// Occurs when the client has successfully reconnected after a connection loss.
        /// </summary>
        public event EventHandler Reconnected;

        /// <summary>
        /// Gets the hub URL.
        /// </summary>
        public String Url { get; }

        /// <summary>
        /// Gets a value indicating whether this client has started.
        /// </summary>
        public bool IsStarted { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether to use the MessagePack protocol over json.
        /// </summary>
        public bool UseMessagePackProtocol { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to enable auto reconnection.
        /// </summary>
        public bool EnableAutoReconnection { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SignalRCoreClient"/> class.
        /// </summary>
        /// <param name="url">The hub URL.</param>
        public SignalRCoreClient(String url)
        {
            UseMessagePackProtocol = true;
            Url = url;
        }

        /// <summary>
        /// Starts the connection.
        /// </summary>
        /// <returns></returns>
        public async Task StartAsync()
        {
            if (IsStarted) return;


            var builder = new HubConnectionBuilder().WithUrl(Url).WithAutomaticReconnect();

            if (UseMessagePackProtocol)
            {
                builder = builder.AddMessagePackProtocol();
            }

            if (EnableAutoReconnection)
            {
                builder = builder.WithAutomaticReconnect(new TimeSpan[]
                {
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(1)
                });
            }

            _connection = builder.Build();

            bool reconnecting = false;

            _connection.Closed += (exception) =>
            {
                if (EnableAutoReconnection)
                {
                    if (reconnecting && _connection.State == HubConnectionState.Disconnected && exception.GetType() == typeof(OperationCanceledException))
                    {
                        Error?.Invoke(this, new ResonanceExceptionEventArgs(exception));
                    }
                }

                return Task.FromResult(true);
            };

            _connection.Reconnecting += (ex) =>
            {
                if (!reconnecting)
                {
                    reconnecting = true;

                    if (EnableAutoReconnection)
                    {
                        Reconnecting?.Invoke(this, new EventArgs());
                    }
                    else
                    {
                        try
                        {
                            Stop();
                            Error?.Invoke(this, new ResonanceExceptionEventArgs(ex));
                        }
                        catch { }
                    }
                }

                return Task.FromResult(true);
            };

            _connection.Reconnected += (msg) =>
            {
                if (EnableAutoReconnection)
                {
                    Reconnected?.Invoke(this, new EventArgs());
                    reconnecting = false;
                }

                return Task.FromResult(true);
            };

            await _connection.StartAsync();
            IsStarted = true;
        }

        /// <summary>
        /// Stops the connection.
        /// </summary>
        public async Task StopAsync()
        {
            if (!IsStarted) return;

            await _connection.StopAsync();
            IsStarted = false;
        }

        /// <summary>
        /// Starts the connection.
        /// </summary>
        public void Start()
        {
            StartAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Stops the connection.
        /// </summary>
        public void Stop()
        {
            StopAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Invokes the specified hub method without expecting a return value.
        /// </summary>
        /// <param name="methodName">Name of the method.</param>
        /// <param name="args">The arguments.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException">This method does not support more than 3 arguments.</exception>
        public Task InvokeAsync(string methodName, params object[] args)
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

        /// <summary>
        /// Invokes the specified hub method and return a value of type T.
        /// </summary>
        /// <typeparam name="T">Type of return value</typeparam>
        /// <param name="methodName">Name of the method.</param>
        /// <param name="args">The arguments.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException">This method does not support more than 3 arguments.</exception>
        public Task<T> InvokeAsync<T>(string methodName, params object[] args)
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

        /// <summary>
        /// Register a callback method for a hub event.
        /// </summary>
        /// <param name="eventName">Name of the event.</param>
        /// <param name="action">The callback.</param>
        /// <returns></returns>
        public IDisposable On(string eventName, Action action)
        {
            return _connection?.On(eventName, action);
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
            return _connection?.On<T>(eventName, action);
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
            return _connection?.On<T1, T2>(eventName, action);
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
            return _connection?.On<T1, T2, T3>(eventName, action);
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
            await StopAsync();
            await _connection.DisposeAsync();
        }
    }
}
