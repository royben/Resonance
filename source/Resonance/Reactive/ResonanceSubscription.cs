using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.Reactive
{
    public class ResonanceSubscription : IDisposable
    {
        private readonly Action _onDispose;

        private TaskCompletionSource<object> _completionSource = new TaskCompletionSource<object>();

        public ResonanceSubscription(Action onDispose, TaskCompletionSource<object> completionSource)
        {
            _completionSource = completionSource;
            _onDispose = onDispose;
        }

        public void Dispose()
        {
            _onDispose?.Invoke();
        }

        public Task WaitAsync()
        {
            return _completionSource.Task;
        }
    }
}
