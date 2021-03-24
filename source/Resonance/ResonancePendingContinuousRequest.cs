using Resonance.Reactive;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Resonance
{
    /// <summary>
    /// Represents a registration of an awaiting continuous request.
    /// </summary>
    /// <seealso cref="Resonance.IResonancePendingRequest" />
    public class ResonancePendingContinuousRequest : IResonancePendingRequest
    {
        /// <summary>
        /// Gets or sets the Resonance request.
        /// </summary>
        public ResonanceRequest Request { get; set; }

        /// <summary>
        /// Gets or sets the request configuration.
        /// </summary>
        public ResonanceContinuousRequestConfig Config { get; set; }

        /// <summary>
        /// Gets or sets the continuous request observable.
        /// </summary>
        public IResonanceObservable ContinuousObservable { get; set; }

        /// <summary>
        /// Gets or sets the request cancellation token.
        /// </summary>
        public CancellationToken CancellationToken { get; set; }
    }
}
