using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance
{
    /// <summary>
    /// Represents the <see cref="IResonanceTransporter.PreviewDecodingInformation"/> event arguments.
    /// </summary>
    /// <seealso cref="System.EventArgs" />
    public class ResonancePreviewDecodingInfoEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the decoding information.
        /// </summary>
        public ResonanceDecodingInformation DecodingInformation { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="ResonancePreviewDecodingInfoEventArgs"/> is handled.
        /// When set to true, will prevent further processing of the data by the transporter.
        /// </summary>
        public bool Handled { get; set; }

        /// <summary>
        /// Gets or sets the incoming raw data.
        /// </summary>
        public byte[] RawData { get; set; }
    }
}
