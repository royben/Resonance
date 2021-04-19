using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Resonance
{
    /// <summary>
    /// Represents a registration of an outgoing response message.
    /// </summary>
    public class ResonancePendingResponse
    {
        public bool IsCompleted { get; private set; }

        /// <summary>
        /// Gets or sets the response.
        /// </summary>
        public ResonanceResponse Response { get; set; }

        /// <summary>
        /// Gets or sets the response completion source.
        /// </summary>
        public TaskCompletionSource<Object> CompletionSource { get; set; }

        /// <summary>
        /// Gets or sets the response configuration.
        /// </summary>
        public ResonanceResponseConfig Config { get; set; }

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
