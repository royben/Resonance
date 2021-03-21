using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance.Reactive
{
    public class ResonanceSubscription : IDisposable
    {
        private readonly Action _onDispose;

        public ResonanceSubscription(Action onDispose)
        {
            _onDispose = onDispose;
        }

        public void Dispose()
        {
            _onDispose?.Invoke();
        }
    }
}
