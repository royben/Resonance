using Resonance.Reactive;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Resonance
{
    /// <summary>
    /// Represents a registration of an awaiting request.
    /// </summary>
    /// <seealso cref="Resonance.IResonancePendingRequest" />
    public class ResonancePendingRequest : IResonancePendingRequest
    {
        public bool IsCompleted { get; private set; }

        /// <summary>
        /// Gets or sets the Resonance request.
        /// </summary>
        public ResonanceMessage Request { get; set; }

        /// <summary>
        /// Gets or sets the request configuration.
        /// </summary>
        public ResonanceRequestConfig Config { get; set; }

        /// <summary>
        /// Gets or sets the request completion source.
        /// </summary>
        public TaskCompletionSource<Object> CompletionSource { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this pending request does not expect any response.
        /// </summary>
        public bool IsWithoutResponse { get; set; }

        public void SetResult(Object result)
        {
            if (!IsCompleted)
            {
                IsCompleted = true;

                if (!CompletionSource.Task.IsCompleted)
                {
                    CompletionSource.SetResult(result);
                }
            }
        }

        public void SetException(Exception exception)
        {
            if (!IsCompleted)
            {
                IsCompleted = true;

                if (!CompletionSource.Task.IsCompleted)
                {
                    CompletionSource.SetException(exception);
                }
            }
        }
    }
}
