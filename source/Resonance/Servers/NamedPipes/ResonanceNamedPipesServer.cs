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
    public class ResonanceNamedPipesServer : ResonanceObject, IDisposable
    {
        private NamedPipeServerStream _pendingPipeStream;

        #region Events

        /// <summary>
        /// Occurs when a new named pipes client has connected.
        /// </summary>
        public event EventHandler<ResonanceNamedPipeServerClientConnectedEventArgs> ClientConnected;

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
        public void Start()
        {
            if (!IsStarted)
            {
                WaitForConnection();
                IsStarted = true;
                LogManager.Log($"{this}: Started...");
            }
        }

        /// <summary>
        /// Stops the server.
        /// </summary>
        public void Stop()
        {
            if (IsStarted)
            {
                IsStarted = false;
                _pendingPipeStream?.Dispose();
                LogManager.Log($"{this}: Stopped.");
            }
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
                    OnClientConnected(server);
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
        protected virtual void OnClientConnected(PipeStream pipe)
        {
            ClientConnected?.Invoke(this, new ResonanceNamedPipeServerClientConnectedEventArgs(pipe));
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
            Stop();
        }

        #endregion
    }
}
