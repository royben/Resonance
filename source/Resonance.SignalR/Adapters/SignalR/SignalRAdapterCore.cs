using Microsoft.AspNetCore.SignalR.Client;
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
    /// Represents a Resonance (.net core) SignalR adapter.
    /// </summary>
    /// <typeparam name="TLocalIdentity">Type of client identity.</typeparam>
    /// <seealso cref="Resonance.Adapters.SignalR.SignalRAdapterBase{TLocalIdentity}" />
    public class SignalRAdapterCore<TLocalIdentity> : SignalRAdapterBase<TLocalIdentity> where TLocalIdentity : IResonanceLocalIdentity<TLocalIdentity>
    {
        private HubConnection _connection;

        /// <summary>
        /// Initializes a new instance of the <see cref="SignalRAdapterCore{TLocalIdentity}"/> class.
        /// </summary>
        /// <param name="url">The remote SignalR service URL.</param>
        /// <param name="hub">The SignalR hub name.</param>
        /// <param name="sessionId">The unique session identifier to join to.</param>
        public SignalRAdapterCore(String url, String sessionId) : base(sessionId)
        {
            Url = url;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SignalRAdapterCore{TLocalIdentity}"/> class.
        /// </summary>
        /// <param name="url">The remote SignalR service URL.</param>
        /// <param name="hub">The SignalR hub name.</param>
        /// <param name="identity">The identity to register.</param>
        public SignalRAdapterCore(String url, TLocalIdentity identity) : base(identity)
        {
            Url = url;
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
                        _connection = new HubConnectionBuilder().WithUrl(Url).Build();

                        if (Mode == SignalRAdapterMode.Accept)
                        {
                            _connection.On(ResonanceHubMethods.SessionCreated, () =>
                            {
                                try
                                {
                                    if (!completed)
                                    {
                                        completed = true;

                                        LogManager.Log($"{this}: Session created ({SessionID})...");
                                        LogManager.Log($"{this}: Connected.");
                                        State = ResonanceComponentState.Connected;

                                        completionSource.SetResult(true);
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

                        _connection.On<byte[]>(ResonanceHubMethods.DataAvailable, (data) => { OnDataAvailable(data); });
                        _connection.StartAsync().GetAwaiter().GetResult();


                        if (Mode == SignalRAdapterMode.Accept)
                        {
                            LogManager.Log($"{this}: Creating session...");
                            SessionID = _connection.InvokeAsync<String>(ResonanceHubMethods.Login, Identity).GetAwaiter().GetResult();
                        }
                        else
                        {
                            LogManager.Log($"{this}: Joining session ({SessionID})...");
                            _connection.InvokeAsync(ResonanceHubMethods.Connect, SessionID).GetAwaiter().GetResult();
                            LogManager.Log($"{this}: Connected.");
                        }

                        if (Mode == SignalRAdapterMode.Connect)
                        {
                            if (!completed)
                            {
                                completed = true;
                                State = ResonanceComponentState.Connected;
                                completionSource.SetResult(true);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (!completed)
                        {
                            completed = true;
                            LogManager.Log(ex, $"{this}: Error occurred while trying to connect.");
                            completionSource.SetException(ex);
                        }
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
            return Task.Factory.StartNew(() =>
            {
                if (State == ResonanceComponentState.Connected)
                {
                    LogManager.Log($"{this}: Disconnecting...");

                    TimeoutTask.StartNew(() =>
                    {
                        try
                        {
                            _connection.StopAsync().GetAwaiter().GetResult();
                            _connection.DisposeAsync().GetAwaiter().GetResult();
                        }
                        catch (Exception ex)
                        {
                            LogManager.Log(ex, $"{this}: Error adapter connection.");
                        }
                    }, TimeSpan.FromSeconds(5));

                    LogManager.Log($"{this}: Disconnected.");
                    State = ResonanceComponentState.Disconnected;
                }
            });
        }

        protected override void OnWrite(byte[] data)
        {
            _connection.InvokeAsync(ResonanceHubMethods.Write, data).GetAwaiter().GetResult();
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

        /// <summary>
        /// Gets the available remote identities available for connection as reported by the remote hub.
        /// </summary>
        /// <typeparam name="TRemoteIdentity">The type of the remote identity.</typeparam>
        /// <param name="url">The hub URL.</param>
        /// <param name="localIdentity">Specify the local identity the remote hub will recognize.</param>
        /// <returns></returns>
        public static async Task<List<TRemoteIdentity>> GetIdentities<TRemoteIdentity>(String url, TLocalIdentity localIdentity) where TRemoteIdentity : IResonanceRemoteIdentity
        {
            var connection = new HubConnectionBuilder().WithUrl(url).Build();
            await connection.StartAsync();
            var identities = await connection.InvokeAsync<List<TRemoteIdentity>>(ResonanceHubMethods.GetIdentities, localIdentity);
            await connection.StopAsync();
            await connection.DisposeAsync();
            return identities;
        }
    }
}
