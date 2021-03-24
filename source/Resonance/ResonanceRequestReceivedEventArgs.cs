using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance
{
    /// <summary>
    /// Represents an <see cref="IResonanceTransporter.RequestReceived"/> events arguments.
    /// </summary>
    /// <seealso cref="Resonance.ResonanceRequestEventArgs" />
    public class ResonanceRequestReceivedEventArgs : ResonanceRequestEventArgs
    {
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="ResonanceRequestReceivedEventArgs"/> is handled.
        /// </summary>
        public bool Handled { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResonanceRequestReceivedEventArgs"/> class.
        /// </summary>
        /// <param name="request">The request.</param>
        public ResonanceRequestReceivedEventArgs(ResonanceRequest request) : base(request)
        {

        }
    }
}
