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
    /// <seealso cref="Resonance.IResonancePendingMessage" />
    public class ResonancePendingRequest : IResonancePendingMessage
    {
        public bool IsCompleted { get; private set; }

        /// <summary>
        /// Gets or sets the Resonance request.
        /// </summary>
        public ResonanceMessage Message { get; set; }

        /// <summary>
        /// Gets or sets the request configuration.
        /// </summary>
        public ResonanceRequestConfig Config { get; set; }

        /// <summary>
        /// Gets or sets the request completion source.
        /// </summary>
        public TaskCompletionSource<Object> CompletionSource { get; set; }

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
