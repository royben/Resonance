using Resonance.Adapters.NamedPipes;
using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.Servers.NamedPipes
{
    /// <summary>
    /// Represents a simple named pipes server.
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    public class ResonanceNamedPipesServer : ResonanceObject, IResonanceListeningServer<NamedPipesAdapter>
    {
        private NamedPipeServerStream _pendingPipeStream;

        #region Events

        /// <summary>
        /// Occurs when a new connection request is available.
        /// </summary>
        public event EventHandler<ResonanceListeningServerConnectionRequestEventArgs<NamedPipesAdapter>> ConnectionRequest;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the name of the pipe.
        /// </summary>
        public string PipeName { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the server has started.
        /// </summary>
        public bool IsStarted { get; private set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ResonanceNamedPipesServer"/> class.
        /// </summary>
        /// <param name="pipeName">Name of the pipe.</param>
        public ResonanceNamedPipesServer(String pipeName)
        {
            PipeName = pipeName;
        }

        #endregion

        #region Start / Stop

        /// <summary>
        /// Starts the server.
        /// </summary>
        public Task Start()
        {
            return Task.Factory.StartNew(() =>
            {
                if (!IsStarted)
                {
                    WaitForConnection();
                    IsStarted = true;
                    Log.Info($"{this}: Started...");
                }
            });
        }

        /// <summary>
        /// Stops the server.
        /// </summary>
        public Task Stop()
        {
            return Task.Factory.StartNew(() =>
            {
                if (IsStarted)
                {
                    IsStarted = false;
                    _pendingPipeStream?.Dispose();
                    Log.Info($"{this}: Stopped.");
                }
            });
        }

        #endregion

        #region Incoming Connection Methods

        private void WaitForConnection()
        {
            _pendingPipeStream = new NamedPipeServerStream(
                PipeName,
                PipeDirection.InOut,
                NamedPipeServerStream.MaxAllowedServerInstances,
                PipeTransmissionMode.Message,
                PipeOptions.Asynchronous);

            _pendingPipeStream.BeginWaitForConnection(PipeConnected, _pendingPipeStream);
        }

        private void PipeConnected(IAsyncResult ar)
        {
            if (!IsStarted) return;

            try
            {
                _pendingPipeStream = null;

                NamedPipeServerStream server = ar.AsyncState as NamedPipeServerStream;

                if (server != null)
                {
                    server.EndWaitForConnection(ar);
                    OnConnectionRequest(server);
                }

                WaitForConnection();
            }
            catch (ObjectDisposedException)
            {
                //Ignore..
            }
        }

        #endregion

        #region Virtual Methods

        /// <summary>
        /// Called when a new named pipes client has connected.
        /// </summary>
        /// <param name="pipe">The pipe.</param>
        protected virtual void OnConnectionRequest(PipeStream pipe)
        {
            ConnectionRequest?.Invoke(this, new ResonanceListeningServerConnectionRequestEventArgs<NamedPipesAdapter>(
                () =>
                {
                    return new NamedPipesAdapter(pipe);
                }, () =>
                {
                    pipe.Dispose();
                }));
        }

        #endregion

        #region ToString

        public override string ToString()
        {
            return $"'{PipeName}' Named Pipes Server";
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            DisposeAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Disposes component resources asynchronously.
        /// </summary>
        /// <returns></returns>
        public Task DisposeAsync()
        {
            return Stop();
        }

        #endregion
    }
}
