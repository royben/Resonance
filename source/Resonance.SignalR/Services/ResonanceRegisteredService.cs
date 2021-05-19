using Microsoft.Extensions.Logging;
using Resonance.Adapters.SignalR;
using Resonance.SignalR.Clients;
using Resonance.SignalR.Hubs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.SignalR.Services
{
    /// <summary>
    /// Represents a Resonance SignalR service.
    /// </summary>
    /// <typeparam name="TCredentials">The type of the credentials.</typeparam>
    /// <typeparam name="TResonanceServiceInformation">The type of the resonance service information.</typeparam>
    /// <typeparam name="TAdapterInformation">The type of the adapter information.</typeparam>
    /// <seealso cref="System.IDisposable" />
    public class ResonanceRegisteredService<TCredentials, TResonanceServiceInformation, TAdapterInformation> : ResonanceObject, IDisposable, IResonanceAsyncDisposable where TResonanceServiceInformation : IResonanceServiceInformation
    {
        private ISignalRClient _client;

        /// <summary>
        /// Occurs when an error has occurred on the internal SignalR client.
        /// </summary>
        public event EventHandler<ResonanceExceptionEventArgs> Error;

        /// <summary>
        /// Occurs when the internal SignalR client is trying to reconnect after a connection loss.
        /// </summary>
        public event EventHandler Reconnecting;

        /// <summary>
        /// Occurs when the internal SignalR client has successfully reconnected after a connection loss.
        /// </summary>
        public event EventHandler Reconnected;

        /// <summary>
        /// Occurs when a remote adapter has requested a connection.
        /// </summary>
        public event EventHandler<ConnectionRequestEventArgs<TCredentials, TAdapterInformation>> ConnectionRequest;

        /// <summary>
        /// Gets the service information.
        /// </summary>
        public TResonanceServiceInformation ServiceInformation { get; private set; }

        /// <summary>
        /// Gets the service credentials used for hub authentication.
        /// </summary>
        public TCredentials Credentials { get; private set; }

        /// <summary>
        /// Gets the SignalR mode (Legacy/Core).
        /// </summary>
        public SignalRMode Mode { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this service is registered.
        /// </summary>
        public bool IsRegistered { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this service is disposed.
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResonanceRegisteredService{TCredentials, TResonanceServiceInformation, TAdapterInformation}"/> class.
        /// </summary>
        /// <param name="credentials">The credentials.</param>
        /// <param name="serviceInformation">The service information.</param>
        /// <param name="mode">The mode.</param>
        /// <param name="signalRClient">The signal r client.</param>
        internal ResonanceRegisteredService(TCredentials credentials, TResonanceServiceInformation serviceInformation, SignalRMode mode, ISignalRClient signalRClient)
        {
            IsRegistered = true;
            Mode = mode;
            Credentials = credentials;
            ServiceInformation = serviceInformation;
            _client = signalRClient;
            _client.Reconnecting -= OnReconnecting;
            _client.Reconnecting += OnReconnecting;
            _client.Reconnected -= OnReconnected;
            _client.Reconnected += OnReconnected;
            _client.Error -= OnError;
            _client.Error += OnError;
            _client.On<String, TAdapterInformation>(ResonanceHubMethods.ConnectionRequest, OnConnectionRequest);
        }

        /// <summary>
        /// Called when the internal SignalR client has disconnected with an error.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="ResonanceExceptionEventArgs"/> instance containing the event data.</param>
        protected virtual void OnError(object sender, ResonanceExceptionEventArgs e)
        {
            IsRegistered = false;
            IsDisposed = false;
            Error?.Invoke(this, e);
        }

        /// <summary>
        /// Called when the internal SignalR client is reconnecting.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected virtual void OnReconnecting(object sender, EventArgs e)
        {
            IsRegistered = false;
            Reconnecting?.Invoke(this, new EventArgs());
        }

        /// <summary>
        /// Called when the internal SignalR client has successfully reconnected.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected async virtual void OnReconnected(object sender, EventArgs e)
        {
            try
            {
                await _client.InvokeAsync(ResonanceHubMethods.Login, Credentials);
                await RegisterAsync();
                Reconnected?.Invoke(this, new EventArgs());
            }
            catch (Exception ex)
            {
                Error?.Invoke(this, new ResonanceExceptionEventArgs(ex));
            }
        }

        /// <summary>
        /// Called when a connection has been accepted.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <returns></returns>
        protected virtual SignalRAdapter<TCredentials> AcceptConnection(string sessionId)
        {
            return SignalRAdapter<TCredentials>.AcceptConnection(Credentials, _client.Url, ServiceInformation.ServiceId, sessionId, Mode);
        }

        /// <summary>
        /// Raises the connection request event.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="adapterInformation">The adapter information.</param>
        protected virtual void OnConnectionRequest(string sessionId, TAdapterInformation adapterInformation)
        {
            ConnectionRequest?.Invoke(this, new ConnectionRequestEventArgs<TCredentials, TAdapterInformation>(AcceptConnection, DeclineConnection)
            {
                SessionId = sessionId,
                RemoteAdapterInformation = adapterInformation
            });
        }

        /// <summary>
        /// Called when a connection has been declined.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        protected virtual void DeclineConnection(string sessionId)
        {
            _client.InvokeAsync(ResonanceHubMethods.DeclineConnection, sessionId);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            DisposeAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Unregisters the service.
        /// </summary>
        public async Task UnregisterAsync()
        {
            if (IsRegistered && !IsDisposed)
            {
                await _client?.InvokeAsync(ResonanceHubMethods.UnregisterService);
            }
        }

        /// <summary>
        /// Unregisters the service.
        /// </summary>
        public void Unregister()
        {
            if (IsRegistered && !IsDisposed)
            {
                UnregisterAsync().GetAwaiter().GetResult();
            }
        }

        /// <summary>
        /// Registers the service.
        /// </summary>
        public async Task RegisterAsync()
        {
            if (!IsRegistered && !IsDisposed)
            {
                await _client?.InvokeAsync(ResonanceHubMethods.RegisterService, ServiceInformation);
            }
        }

        /// <summary>
        /// Registers the service.
        /// </summary>
        public void Register()
        {
            if (!IsRegistered && !IsDisposed)
            {
                RegisterAsync().GetAwaiter().GetResult();
            }
        }

        /// <summary>
        /// Disposes component resources asynchronously.
        /// </summary>
        /// <returns></returns>
        public async Task DisposeAsync()
        {
            if (!IsDisposed)
            {
                try
                {
                    await UnregisterAsync();
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error occurred while disposing the registered service. Unregister service failed.");
                }

                IsDisposed = true;
                await _client?.StopAsync();
                await _client?.DisposeAsync();
                _client = null;
            }
        }
    }
}
