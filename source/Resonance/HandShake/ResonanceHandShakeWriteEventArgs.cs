using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance.HandShake
{
    /// <summary>
    /// Represents an <see cref="IResonanceHandShakeNegotiator.WriteHandShake"/> event arguments.
    /// </summary>
    /// <seealso cref="System.EventArgs" />
    public class ResonanceHandShakeWriteEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the data to be written.
        /// </summary>
        public byte[] Data { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResonanceHandShakeWriteEventArgs"/> class.
        /// </summary>
        /// <param name="data">The data to be written.</param>
        public ResonanceHandShakeWriteEventArgs(byte[] data)
        {
            Data = data;
        }
    }
}
