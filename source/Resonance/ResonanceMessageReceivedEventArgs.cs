using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance
{
    /// <summary>
    /// Represents a message received events arguments.
    /// </summary>
    /// <seealso cref="Resonance.ResonanceMessageEventArgs" />
    public class ResonanceMessageReceivedEventArgs : ResonanceMessageEventArgs
    {
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="ResonanceMessageReceivedEventArgs"/> is handled.
        /// </summary>
        public bool Handled { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResonanceMessageReceivedEventArgs"/> class.
        /// </summary>
        /// <param name="transporter">The request transporter</param>
        /// <param name="message">The resonance message.</param>
        public ResonanceMessageReceivedEventArgs(IResonanceTransporter transporter, ResonanceMessage message) : base(transporter, message)
        {

        }
    }
}
