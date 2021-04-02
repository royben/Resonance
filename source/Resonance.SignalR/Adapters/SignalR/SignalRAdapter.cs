using Resonance.SignalR;
using Resonance.SignalR.Clients;
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
        private ISignalRClient _client;
        private TCredentials _credentials;

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

        /// <summary>
        /// Gets or sets the SignalR mode (legacy/core).
        /// </summary>
        public SignalRMode Mode { get; private set; }

        public SignalRAdapter()
        {
            ConnectionTimeout = TimeSpan.FromSeconds(60);
        }

        public SignalRAdapter(TCredentials credentials, String url, String serviceId, SignalRMode mode) : this()
        {
            Mode = mode;
            Url = url;
            ServiceId = serviceId;
            _credentials = credentials;
            Role = SignalRAdapterRole.Connect;
        }

        public static SignalRAdapter<TCredentials> AcceptConnection(TCredentials credentials, String url, String serviceId, String sessionId, SignalRMode mode)
        {
            SignalRAdapter<TCredentials> adapter = new SignalRAdapter<TCredentials>();

            adapter.Mode = mode;
            adapter.Url = url;
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
                        _client = SignalRClientFactory.Default.Create(Mode, Url);
                        _client.Start().GetAwaiter().GetResult();

                        if (Role == SignalRAdapterRole.Connect)
                        {
                            _client.On(ResonanceHubMethods.Connected, () =>
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

                            _client.On(ResonanceHubMethods.Declined, () =>
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

                        _client.On(ResonanceHubMethods.Disconnected, () =>
                        {
                            //OnDisconnect(false); //Don't know what to do here.. We already have the resonance disconnection message.
                            //Maybe just raise an event..
                        });

                        LogManager.Log($"{this}: Logging in...");
                        _client.Invoke(ResonanceHubMethods.Login, _credentials).GetAwaiter().GetResult();

                        if (Role == SignalRAdapterRole.Connect)
                        {
                            LogManager.Log($"{this}: Connecting service ({ServiceId})...");
                            SessionId = _client.Invoke<String>(ResonanceHubMethods.Connect, ServiceId).GetAwaiter().GetResult();
                        }
                        else
                        {
                            LogManager.Log($"{this}: Accepting connection ({SessionId})...");
                            _client.Invoke(ResonanceHubMethods.AcceptConnection, SessionId).GetAwaiter().GetResult();
                            LogManager.Log($"{this}: Connected.");

                            if (!completed)
                            {
                                completed = true;
                                State = ResonanceComponentState.Connected;
                                completionSource.SetResult(true);
                            }
                        }

                        _client.On<byte[]>(ResonanceHubMethods.DataAvailable, (data) => { OnDataAvailable(data); });
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
                            _client.Invoke(ResonanceHubMethods.Disconnect).GetAwaiter().GetResult();
                        }
                        _client.Stop();
                        _client.Dispose();
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
            _client.Invoke(ResonanceHubMethods.Write, data).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return $"{base.ToString()} ({Url})";
        }
    }
}
