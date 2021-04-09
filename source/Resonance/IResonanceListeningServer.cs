using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Resonance
{
    /// <summary>
    /// Represents a server that accepts incoming connections and exposes them as adapters.
    /// </summary>
    /// <typeparam name="TAdapter">The type of the adapter.</typeparam>
    /// <seealso cref="System.IDisposable" />
    /// <seealso cref="Resonance.IResonanceAsyncDisposable" />
    public interface IResonanceListeningServer<TAdapter> : IDisposable, IResonanceAsyncDisposable where TAdapter : IResonanceAdapter
    {
        /// <summary>
        /// Occurs when a new connection request is available.
        /// </summary>
        event EventHandler<ResonanceListeningServerConnectionRequestEventArgs<TAdapter>> ConnectionRequest;

        /// <summary>
        /// Gets a value indicating whether this server is currently listening for incoming connections.
        /// </summary>
        bool IsStarted { get; }

        /// <summary>
        /// Start listening for incoming connections.
        /// </summary>
        Task Start();

        /// <summary>
        /// Stop listening for incoming connections.
        /// </summary>
        Task Stop();
    }
}
