using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance.Reactive
{
    public class ResonanceObserver<T> : IObserver<T>
    {
        private Action<T> _onNext;
        private Action<Exception> _onError;
        private Action _onComplete;

        public bool IsCompleted { get; private set; }

        public void OnCompleted()
        {
            if (!IsCompleted)
            {
                IsCompleted = true;
                _onComplete?.Invoke();
            }
        }

        public void OnError(Exception error)
        {
            if (!IsCompleted)
            {
                IsCompleted = true;
                _onError?.Invoke(error);
            }
        }

        public void OnNext(T value)
        {
            if (!IsCompleted)
            {
                _onNext?.Invoke(value);
            }
        }
    }
}
