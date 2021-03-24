using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.Reactive
{
    /// <summary>
    /// Represents a continuous message observable.
    /// </summary>
    public interface IResonanceObservable
    {
        /// <summary>
        /// Called when a new response has been received.
        /// </summary>
        /// <param name="response">The response.</param>
        void OnNext(Object response);
        /// <summary>
        /// Called when the response has returned with an error.
        /// </summary>
        /// <param name="exception">The exception.</param>
        void OnError(Exception exception);
        /// <summary>
        /// Called when the response is marked as completed.
        /// </summary>
        void OnCompleted();
        /// <summary>
        /// Gets a value indicating whether the continuous request has completed and is no longer accepting responses.
        /// </summary>
        bool IsCompleted { get; }

        /// <summary>
        /// Gets a value indicating whether at least one response has arrived.
        /// </summary>
        bool FirstMessageArrived { get; }

        /// <summary>
        /// Gets the last response date time.
        /// </summary>
        DateTime LastResponseTime { get; }
    }
}
