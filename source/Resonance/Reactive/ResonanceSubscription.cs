using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.Reactive
{
    /// <summary>
    /// Represents a Resonance continuous request subscription.
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    public class ResonanceSubscription : IDisposable
    {
        private readonly Action _onDispose;

        private TaskCompletionSource<object> _completionSource;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResonanceSubscription"/> class.
        /// </summary>
        public ResonanceSubscription(Action onDispose, TaskCompletionSource<object> completionSource)
        {
            _completionSource = completionSource;
            _onDispose = onDispose;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _onDispose?.Invoke();
        }

        /// <summary>
        /// Asynchronously waits for the continuous request to marked as completed.
        /// </summary>
        /// <returns></returns>
        public Task WaitAsync()
        {
            return _completionSource.Task;
        }
    }
}
