using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance
{
    /// <summary>
    /// Represents a Resonance adapter capable of connecting, reading and writing data.
    /// </summary>
    /// <seealso cref="Resonance.IResonanceComponent" />
    /// <seealso cref="Resonance.IResonanceStateComponent" />
    /// <seealso cref="Resonance.IResonanceConnectionComponent" />
    public interface IResonanceAdapter : IResonanceComponent, IResonanceStateComponent, IResonanceConnectionComponent , IDisposable, IResonanceAsyncDisposableComponent
    {
        /// <summary>
        /// Gets the total bytes received.
        /// </summary>
        long TotalBytesReceived { get; }

        /// <summary>
        /// Gets the total bytes sent.
        /// </summary>
        long TotalBytesSent { get; }

        /// <summary>
        /// Gets the current transfer rate.
        /// </summary>
        long TransferRate { get; }

        /// <summary>
        /// Gets or sets a value indicating whether to enable compression/decompression of data.
        /// </summary>
        bool EnableCompression { get; set; }

        /// <summary>
        /// Gets or sets the adapter data writing mode.
        /// </summary>
        ResonanceAdapterWriteMode WriteMode { get; set; }

        /// <summary>
        /// Gets the queue write mode interval when <see cref="WriteMode"/> is set to <see cref="ResonanceAdapterWriteMode.Queue"/>.
        /// </summary>
        TimeSpan QueueWriteModeInterval { get; set; }

        /// <summary>
        /// Writes the specified encoded data.
        /// </summary>
        /// <param name="data">The data.</param>
        void Write(byte[] data);

        /// <summary>
        /// Occurs when a new encoded data is available.
        /// </summary>
        event EventHandler<ResonanceAdapterDataAvailableEventArgs> DataAvailable;
    }
}
