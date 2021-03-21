using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance
{
    /// <summary>
    /// Represents a transport adapter capable of connecting, writing and receiving data from a stream.
    /// </summary>
    /// <seealso cref="Tango.Transport.ITransportComponent" />
    public interface IResonanceAdapter : IResonanceComponent, INotifyPropertyChanged
    {
        ResonanceComponentState State { get; }

        event EventHandler<ResonanceComponentState> StateChanged;

        /// <summary>
        /// Gets the last failed state exception/reason.
        /// </summary>
        Exception FailedStateException { get; }

        /// <summary>
        /// Gets the total bytes received.
        /// </summary>
        long TotalBytesReceived { get; }

        /// <summary>
        /// Gets the total bytes sent.
        /// </summary>
        long TotalBytesSent { get; }

        /// <summary>
        /// Gets the adapter current transfer rate.
        /// </summary>
        long TransferRate { get; }

        /// <summary>
        /// Gets or sets a value indicating whether to enable compression/decompression of data.
        /// </summary>
        bool EnableCompression { get; set; }

        /// <summary>
        /// Writes the specified data to the stream.
        /// </summary>
        /// <param name="data">The data.</param>
        void Write(byte[] data);

        /// <summary>
        /// Occurs when new data is available.
        /// </summary>
        event EventHandler<byte[]> DataAvailable;

        /// <summary>
        /// Gets or sets the adapter address.
        /// </summary>
        String Address { get; set; }
    }
}
