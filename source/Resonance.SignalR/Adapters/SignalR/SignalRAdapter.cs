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
    /// Represents a Resonance SignalR adapter with support for both SignalR and SignalR Core.
    /// This adapter is designed to communicate with an <see cref="IResonanceHub{TCredentials, TServiceInformation, TReportedServiceInformation, TAdapterInformation}"/> implementation.
    /// </summary>
    /// <typeparam name="TCredentials">A type that determined the object that will be used to authenticate this adapter with the remote ResonanceHub.</typeparam>
    /// <seealso cref="Resonance.ResonanceAdapter" />
    public class SignalRAdapter<TCredentials> : ResonanceAdapter
    {
        private ISignalRClient _client;
        private TCredentials _credentials;

        /// <summary>
        /// Gets the URL of the SignalR service.
        /// When using <see cref="F:Resonance.SignalR.SignalRMode.Legacy" />, this should be url/hub.
        /// </summary>
        public string Url { get; private set; }

        /// <summary>
        /// Gets the remote <see cref="T:Resonance.SignalR.IResonanceServiceInformation" /> id.
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
        /// Gets the adapter role.
        /// Meaning, whether this adapter has requested or accepted the session.
        /// </summary>
        public SignalRAdapterRole Role { get; private set; }

        /// <summary>
        /// Gets or sets the SignalR mode (legacy/core).
        /// Legacy: The remote SignalR hub is implemented using .NET Framework.
        /// Core: The remote SignalR hub is implemented using .NET Core.
        /// </summary>
        public SignalRMode Mode { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SignalRAdapter{TCredentials}"/> class.
        /// </summary>
        public SignalRAdapter()
        {
            ConnectionTimeout = TimeSpan.FromSeconds(10);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SignalRAdapter{TCredentials}"/> class.
        /// </summary>
        /// <param name="credentials">The credentials that will be used to authenticate with the remote hub.</param>
        /// <param name="url">The hub URL.</param>
        /// <param name="serviceId">The remote service identifier.</param>
        /// <param name="mode">The SignalR mode.</param>
        public SignalRAdapter(TCredentials credentials, String url, String serviceId, SignalRMode mode) : this()
        {
            Mode = mode;
            Url = url;
            ServiceId = serviceId;
            _credentials = credentials;
            Role = SignalRAdapterRole.Connect;
        }

        /// <summary>
        /// Returns an initialized adapter based on parameters that should be provided by a <see cref="Resonance.SignalR.Services.ResonanceRegisteredService{TCredentials, TResonanceServiceInformation, TAdapterInformation}.ConnectionRequest"/> event arguments.
        /// </summary>
        /// <param name="credentials">The credentials used to authenticate this adapter with the remote service.</param>
        /// <param name="url">The hub URL.</param>
        /// <param name="serviceId">The service identifier.</param>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="mode">The SignalR mode.</param>
        /// <returns></returns>
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

                                        State = ResonanceComponentState.Connected;
                                        completionSource.SetResult(true);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    if (!completed)
                                    {
                                        Log.Error(ex, $"{this}: Error occurred after successful connection.");
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

                                        Log.Error(ex, $"{this}: Error occurred after session created.");
                                        completionSource.SetException(ex);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    if (!completed)
                                    {
                                        Log.Error(ex, $"{this}: Error occurred after session created.");
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

                        Log.Info($"{this}: Authenticating with the remote hub...");
                        _client.Invoke(ResonanceHubMethods.Login, _credentials).GetAwaiter().GetResult();

                        if (Role == SignalRAdapterRole.Connect)
                        {
                            Log.Info($"{this}: Connecting to service ({ServiceId})...");
                            SessionId = _client.Invoke<String>(ResonanceHubMethods.Connect, ServiceId).GetAwaiter().GetResult();
                        }
                        else
                        {
                            Log.Info($"{this}: Accepting connection ({SessionId})...");
                            _client.Invoke(ResonanceHubMethods.AcceptConnection, SessionId).GetAwaiter().GetResult();

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
                        Log.Error(ex, $"{this}: Error occurred while trying to connect.");
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
                    Log.Error(ex, $"{this}: Error occurred while disconnecting.");
                }

                Log.Info($"{this}: Disconnected.");
                State = ResonanceComponentState.Disconnected;

                if (!notify)
                {
                    OnFailed(new RemoteAdapterDisconnectedException(), "The remote SignalR adapter has disconnected.");
                }
            });
        }

        protected override void OnWrite(byte[] data)
        {
            _client.Invoke(ResonanceHubMethods.Write, data).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Returns the string representation of this adapter.
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
