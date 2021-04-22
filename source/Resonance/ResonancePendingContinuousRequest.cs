using Resonance.Reactive;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Resonance
{
    /// <summary>
    /// Represents a registration of an awaiting continuous request.
    /// </summary>
    /// <seealso cref="Resonance.IResonancePendingRequest" />
    public class ResonancePendingContinuousRequest : IResonancePendingRequest
    {
        private ResonanceContinuousResponseDispatcher _dispatcher;

        public bool IsCompleted { get; private set; }

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

        /// <summary>
        /// Initializes a new instance of the <see cref="ResonancePendingContinuousRequest"/> class.
        /// </summary>
        public ResonancePendingContinuousRequest()
        {
            _dispatcher = new ResonanceContinuousResponseDispatcher();
        }

        /// <summary>
        /// Enqueues a response.
        /// </summary>
        /// <param name="response">The response.</param>
        public void OnNext(Object response)
        {
            if (!IsCompleted)
            {
                _dispatcher.Enqueue(() =>
                {
                    ContinuousObservable.OnNext(response);
                });
            }
        }

        /// <summary>
        /// Enqueues an error response.
        /// </summary>
        /// <param name="exception">The exception.</param>
        public void OnError(Exception exception)
        {
            if (!IsCompleted)
            {
                IsCompleted = true;

                _dispatcher.Enqueue(() =>
                {
                    ContinuousObservable.OnError(exception);
                });

                _dispatcher.Dispose();
            }
        }

        /// <summary>
        /// Enqueues a completion signal.
        /// </summary>
        public void OnCompleted()
        {
            if (!IsCompleted)
            {
                IsCompleted = true;

                _dispatcher.Enqueue(() =>
                {
                    ContinuousObservable.OnCompleted();
                });

                _dispatcher.Dispose();
            }
        }
    }
}
