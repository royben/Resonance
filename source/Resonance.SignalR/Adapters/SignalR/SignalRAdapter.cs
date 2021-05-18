using Microsoft.Extensions.Logging;
using Resonance.SignalR;
using Resonance.SignalR.Clients;
using Resonance.SignalR.Hubs;
using Resonance.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        /// <summary>
        /// Occurs when the internal SignalR client is trying to reconnect after a connection loss.
        /// </summary>
        public event EventHandler Reconnecting;

        /// <summary>
        /// Occurs when the internal SignalR client has successfully reconnected after a connection loss.
        /// </summary>
        public event EventHandler Reconnected;

        /// <summary>
        /// Gets the credentials used to authenticate with the remote Resonance SignalR hub.
        /// </summary>
        public TCredentials Credentials { get; private set; }

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
            Credentials = credentials;
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
            adapter.Credentials = credentials;
            adapter.Role = SignalRAdapterRole.Accept;

            return adapter;
        }

        /// <summary>
        /// Called when the adapter is connecting.
        /// </summary>
        /// <returns></returns>
        protected override Task OnConnect()
        {
            bool completed = false;

            TaskCompletionSource<object> completionSource = new TaskCompletionSource<object>();

            Task.Factory.StartNew(() =>
            {
                try
                {
                    _client = SignalRClientFactory.Default.Create(Mode, Url);
                    _client.StartAsync().GetAwaiter().GetResult();

                    if (Role == SignalRAdapterRole.Connect)
                    {
                        _client.On(ResonanceHubMethods.Connected, () =>
                        {
                            try
                            {
                                if (!completed)
                                {
                                    completed = true;
                                    completionSource.SetResult(true);
                                }
                            }
                            catch (Exception ex)
                            {
                                if (!completed)
                                {
                                    Logger.LogError(ex, "Error occurred after successful connection.");
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

                                    Logger.LogError(ex, "Error occurred after session created.");
                                    completionSource.SetException(ex);
                                }
                            }
                            catch (Exception ex)
                            {
                                if (!completed)
                                {
                                    Logger.LogError(ex, "Error occurred after session created.");
                                    completed = true;
                                    completionSource.SetException(ex);
                                }
                            }
                        });
                    }

                    _client.On(ResonanceHubMethods.Disconnected, () =>
                    {
                        if (State == ResonanceComponentState.Connected)
                        {
                            //OnDisconnect(false); //Don't know what to do here.. We already have the resonance disconnection message.
                            //Maybe just raise an event..
                        }
                    });

                    _client.On(ResonanceHubMethods.ServiceDown, () =>
                    {
                        OnFailed(new ServiceDownException());
                    });

                    Logger.LogInformation("Authenticating with the remote hub {HubUrl}...", _client.Url);
                    _client.InvokeAsync(ResonanceHubMethods.Login, Credentials).GetAwaiter().GetResult();

                    if (Role == SignalRAdapterRole.Connect)
                    {
                        Logger.LogInformation("Connecting to service {ServiceId}...", ServiceId);
                        SessionId = _client.InvokeAsync<String>(ResonanceHubMethods.Connect, ServiceId).GetAwaiter().GetResult();
                    }
                    else
                    {
                        Logger.LogInformation("Accepting connection {SessionId}...", SessionId);
                        _client.InvokeAsync(ResonanceHubMethods.AcceptConnection, SessionId).GetAwaiter().GetResult();

                        if (!completed)
                        {
                            completed = true;
                            completionSource.SetResult(true);
                        }
                    }

                    _client.On<byte[]>(ResonanceHubMethods.DataAvailable, (data) => { OnDataAvailable(data); });

                    _client.Error += OnError;
                    _client.Reconnecting += OnReconnecting;
                    _client.Reconnected += OnReconnected;
                }
                catch (Exception ex)
                {
                    completed = true;
                    Logger.LogError(ex, "Error occurred while trying to connect.");
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

        /// <summary>
        /// Called when the internal SignalR client has failed to reconnect.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="ResonanceExceptionEventArgs"/> instance containing the event data.</param>
        protected virtual void OnError(object sender, ResonanceExceptionEventArgs e)
        {
            if (State == ResonanceComponentState.Connected)
            {
                OnFailed(e.Exception, "The internal SignalR client has lost the connection and failed to reconnect.");
            }
        }

        /// <summary>
        /// Called when the internal SignalR client is trying to reconnect after a connection loss.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected virtual void OnReconnecting(object sender, EventArgs e)
        {
            if (State == ResonanceComponentState.Connected)
            {
                Reconnecting?.Invoke(this, e);
            }
        }

        /// <summary>
        /// Called when the internal SignalR client has successfully reconnected.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected virtual void OnReconnected(object sender, EventArgs e)
        {
            if (State == ResonanceComponentState.Connected)
            {
                Reconnected?.Invoke(this, e);
            }
        }

        /// <summary>
        /// Called when the adapter is disconnecting.
        /// </summary>
        /// <returns></returns>
        protected override Task OnDisconnect()
        {
            return OnDisconnect(!IsFailing);
        }

        /// <summary>
        /// Called when the adapter is disconnecting.
        /// </summary>
        /// <param name="notify">if set to <c>true</c> to notify the other side about the disconnection.</param>
        private async Task OnDisconnect(bool notify)
        {
            try
            {
                if (notify)
                {
                    await _client.InvokeAsync(ResonanceHubMethods.Disconnect);
                }
                await _client.StopAsync();
                await _client.DisposeAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Error occurred while disconnecting.");
            }

            Logger.LogInformation("Disconnected.");

            if (!notify && !IsFailing)
            {
                OnFailed(new RemoteAdapterDisconnectedException(), "The remote SignalR adapter has disconnected.");
            }
        }

        /// <summary>
        /// Called when the adapter is writing.
        /// </summary>
        /// <param name="data">The data.</param>
        protected override void OnWrite(byte[] data)
        {
            _client.InvokeAsync(ResonanceHubMethods.Write, data).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Returns the string representation of this adapter.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return $"SignalRAdapter {ComponentCount}";
        }
    }
}
