using Microsoft.AspNet.SignalR.Client;
using Resonance.SignalR;
using Resonance.SignalR.Hubs;
using Resonance.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.Adapters.SignalR
{
    /// <summary>
    /// Represents a Resonance (legacy) SignalR adapter.
    /// </summary>
    /// <typeparam name="TLocalIdentity">Type of client identity.</typeparam>
    /// <seealso cref="Resonance.Adapters.SignalR.SignalRAdapterBase{T}" />
    public class SignalRAdapter<TCredentials> : ResonanceAdapter, ISignalRAdapter<TCredentials>
    {
        private IHubProxy _proxy;
        private HubConnection _connection;
        private TCredentials _credentials;

        /// <summary>
        /// Gets or sets the hub.
        /// </summary>
        public string Hub { get; protected set; }

        /// <summary>
        /// Gets the URL of the SignalR service.
        /// </summary>
        public string Url { get; private set; }

        /// <summary>
        /// Gets the service identifier.
        /// </summary>
        public string ServiceId { get; private set; }

        /// <summary>
        /// Gets the remote session identifier.
        /// </summary>
        public string SessionId { get; private set; }

        /// <summary>
        /// Gets or sets the adapter connection timeout.
        /// </summary>
        public TimeSpan ConnectionTimeout { get; set; }

        /// <summary>
        /// Gets the adapter mode.
        /// </summary>
        public SignalRAdapterRole Role { get; private set; }

        public SignalRAdapter()
        {
            ConnectionTimeout = TimeSpan.FromSeconds(60);
        }

        public SignalRAdapter(String url, String hub, String serviceId, TCredentials credentials) : this()
        {
            Url = url;
            Hub = hub;
            ServiceId = serviceId;
            _credentials = credentials;
            Role = SignalRAdapterRole.Connect;
        }

        public static SignalRAdapter<TCredentials> AcceptConnection(String url, String hub, String serviceId, String sessionId, TCredentials credentials)
        {
            SignalRAdapter<TCredentials> adapter = new SignalRAdapter<TCredentials>();
            adapter.Url = url;
            adapter.Hub = hub;
            adapter.ServiceId = serviceId;
            adapter.SessionId = sessionId;
            adapter._credentials = credentials;
            adapter.Role = SignalRAdapterRole.Accept;

            return adapter;
        }

        protected override Task OnConnect()
        {
            if (State != ResonanceComponentState.Connected)
            {
                LogManager.Log($"{this}: Connecting SignalR adapter...");

                bool completed = false;

                TaskCompletionSource<object> completionSource = new TaskCompletionSource<object>();

                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        _connection = new HubConnection(Url);
                        _proxy = _connection.CreateHubProxy(Hub);

                        if (Role == SignalRAdapterRole.Connect)
                        {
                            _proxy.On(ResonanceHubMethods.Connected, () =>
                            {
                                try
                                {
                                    if (!completed)
                                    {
                                        completed = true;

                                        LogManager.Log($"{this}: Connected.");
                                        State = ResonanceComponentState.Connected;

                                        completionSource.SetResult(true);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    if (!completed)
                                    {
                                        LogManager.Log(ex, $"{this}: Error occurred after successful connection.");
                                        completed = true;
                                        completionSource.SetException(ex);
                                    }
                                }
                            });

                            _proxy.On(ResonanceHubMethods.Declined, () =>
                            {
                                try
                                {
                                    if (!completed)
                                    {
                                        completed = true;

                                        var ex = new ConnectionDeclinedException();

                                        LogManager.Log(ex, $"{this}: Error occurred after session created.");
                                        completionSource.SetException(ex);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    if (!completed)
                                    {
                                        LogManager.Log(ex, $"{this}: Error occurred after session created.");
                                        completed = true;
                                        completionSource.SetException(ex);
                                    }
                                }
                            });
                        }

                        _proxy.On(ResonanceHubMethods.Disconnected, () =>
                        {
                            //OnDisconnect(false); //Don't know what to do here.. We already have the resonance disconnection message.
                            //Maybe just raise an event..
                        });

                        _connection.StateChanged += async (x) =>
                        {
                            try
                            {
                                if (x.NewState == ConnectionState.Connected)
                                {
                                    LogManager.Log($"{this}: Logging in...");
                                    await _proxy.Invoke(ResonanceHubMethods.Login, _credentials);

                                    if (Role == SignalRAdapterRole.Connect)
                                    {
                                        LogManager.Log($"{this}: Connecting service ({ServiceId})...");
                                        SessionId = await _proxy.Invoke<String>(ResonanceHubMethods.Connect, ServiceId);
                                    }
                                    else
                                    {
                                        LogManager.Log($"{this}: Accepting connection ({SessionId})...");
                                        await _proxy.Invoke(ResonanceHubMethods.AcceptConnection, SessionId);
                                        LogManager.Log($"{this}: Connected.");

                                        if (!completed)
                                        {
                                            completed = true;
                                            State = ResonanceComponentState.Connected;
                                            completionSource.SetResult(true);
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                if (!completed)
                                {
                                    completed = true;
                                    LogManager.Log(ex, $"{this}: Error occurred on connection state changed event.");
                                    completionSource.SetException(ex);
                                }
                            }
                        };

                        _proxy.On<byte[]>(ResonanceHubMethods.DataAvailable, (data) => { OnDataAvailable(data); });
                        _connection.Start();
                    }
                    catch (Exception ex)
                    {
                        completed = true;
                        LogManager.Log(ex, $"{this}: Error occurred while trying to connect.");
                        completionSource.SetException(ex);
                    }
                });

                TimeoutTask.StartNew(() =>
                {
                    if (!completed)
                    {
                        completed = true;
                        completionSource.SetException(new TimeoutException("Could not connect after the given timeout."));
                    }

                }, ConnectionTimeout);

                return completionSource.Task;
            }

            return Task.FromResult(true);
        }

        protected override Task OnDisconnect()
        {
            return OnDisconnect(true);
        }

        private Task OnDisconnect(bool notify)
        {
            return Task.Factory.StartNew(() =>
            {
                if (State == ResonanceComponentState.Connected)
                {
                    LogManager.Log($"{this}: Disconnecting...");

                    try
                    {
                        if (notify)
                        {
                            _proxy.Invoke(ResonanceHubMethods.Disconnect).GetAwaiter().GetResult();
                        }
                        _connection.Stop();
                        _connection.Dispose();
                    }
                    catch (Exception ex)
                    {
                        LogManager.Log(ex, $"{this}: Error occurred while disconnecting.");
                    }

                    LogManager.Log($"{this}: Disconnected.");
                    State = ResonanceComponentState.Disconnected;

                    if (!notify)
                    {
                        OnFailed(new RemoteAdapterDisconnectedException());
                    }
                }
            });
        }

        protected override void OnWrite(byte[] data)
        {
            _proxy.Invoke(ResonanceHubMethods.Write, data).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return $"{base.ToString()} ({Url}/{Hub})";
        }
    }
}
