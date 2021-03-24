using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance.Reactive
{
    /// <summary>
    /// Represents a Resonance continuous request observer.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <seealso cref="System.IObserver{T}" />
    public class ResonanceObserver<T> : IObserver<T>
    {
        private Action<T> _onNext;
        private Action<Exception> _onError;
        private Action _onComplete;

        /// <summary>
        /// Gets a value indicating whether this instance is completed.
        /// </summary>
        public bool IsCompleted { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResonanceObserver{T}"/> class.
        /// </summary>
        public ResonanceObserver(Action<T> onNext, Action<Exception> onError, Action onComplete)
        {
            _onNext = onNext;
            _onError = onError;
            _onComplete = onComplete;
        }

        /// <summary>
        /// Notifies the observer that the provider has finished sending push-based notifications.
        /// </summary>
        public void OnCompleted()
        {
            if (!IsCompleted)
            {
                IsCompleted = true;
                _onComplete?.Invoke();
            }
        }

        /// <summary>
        /// Notifies the observer that the provider has experienced an error condition.
        /// </summary>
        /// <param name="error">An object that provides additional information about the error.</param>
        public void OnError(Exception error)
        {
            if (!IsCompleted)
            {
                IsCompleted = true;
                _onError?.Invoke(error);
            }
        }

        /// <summary>
        /// Provides the observer with new data.
        /// </summary>
        /// <param name="value">The current notification information.</param>
        public void OnNext(T value)
        {
            if (!IsCompleted)
            {
                _onNext?.Invoke(value);
            }
        }
    }
}
