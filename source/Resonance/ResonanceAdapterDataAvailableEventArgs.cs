using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance
{
    /// <summary>
    /// Represents an <see cref="IResonanceAdapter.DataAvailable"/> event arguments.
    /// </summary>
    /// <seealso cref="System.EventArgs" />
    public class ResonanceAdapterDataAvailableEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the incoming encoded data.
        /// </summary>
        public byte[] Data { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResonanceAdapterDataAvailableEventArgs"/> class.
        /// </summary>
        /// <param name="data">Incoming encoded data.</param>
        public ResonanceAdapterDataAvailableEventArgs(byte[] data)
        {
            Data = data;
        }
    }
}
