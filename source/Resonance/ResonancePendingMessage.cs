using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Resonance
{
    /// <summary>
    /// Represents a pending message.
    /// </summary>
    /// <seealso cref="Resonance.IResonancePendingMessage" />
    public class ResonancePendingMessage : IResonancePendingMessage
    {
        public bool IsCompleted { get; private set; }

        /// <summary>
        /// Gets or sets the Resonance message.
        /// </summary>
        public ResonanceMessage Message { get; set; }

        /// <summary>
        /// Gets or sets the message configuration.
        /// </summary>
        public ResonanceMessageConfig Config { get; set; }

        /// <summary>
        /// Gets or sets the request completion source.
        /// </summary>
        public TaskCompletionSource<Object> CompletionSource { get; set; }

        public void SetResult()
        {
            if (!IsCompleted)
            {
                IsCompleted = true;

                if (!CompletionSource.Task.IsCompleted)
                {
                    CompletionSource.SetResult(true);
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
