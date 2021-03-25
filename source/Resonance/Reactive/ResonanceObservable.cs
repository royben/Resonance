using Resonance.Threading;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.Reactive
{
    /// <summary>
    /// Represents a continuous message observable.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <seealso cref="System.IObservable{T}" />
    /// <seealso cref="Resonance.Reactive.IResonanceObservable" />
    public class ResonanceObservable<T> : IObservable<T>, IResonanceObservable
    {
        private readonly ConcurrentList<IObserver<T>> _observers = new ConcurrentList<IObserver<T>>();
        private TaskCompletionSource<object> _completionSource;

        /// <summary>
        /// Gets a value indicating whether the continuous request has completed and is no longer accepting responses.
        /// </summary>
        public bool IsCompleted { get; private set; }

        /// <summary>
        /// Gets a value indicating whether at least one response has arrived.
        /// </summary>
        public bool FirstMessageArrived { get; private set; }

        /// <summary>
        /// Gets the last response date time.
        /// </summary>
        public DateTime LastResponseTime { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResonanceObservable{T}"/> class.
        /// </summary>
        public ResonanceObservable()
        {
            _completionSource = new TaskCompletionSource<object>();
        }

        /// <summary>
        /// Notifies the provider that an observer is to receive notifications.
        /// </summary>
        /// <param name="observer">The object that is to receive notifications.</param>
        /// <returns>
        /// A reference to an interface that allows observers to stop receiving notifications before the provider has finished sending them.
        /// </returns>
        public IDisposable Subscribe(IObserver<T> observer)
        {
            ResonanceSubscription subscription = new ResonanceSubscription(() => _observers.Remove(observer), _completionSource);
            _observers.Add(observer);
            return subscription;
        }

        /// <summary>
        /// Notifies the provider that an observer is to receive notifications.
        /// </summary>
        /// <param name="next">Provide a delegate for an incoming responses.</param>
        /// <param name="error">Provide a delegate for an error response.</param>
        /// <param name="completed">Provide a delegate for when the continuous request completes.</param>
        /// <returns>
        /// A reference to an interface that allows observers to stop receiving notifications before the provider has finished sending them.
        /// </returns>
        public ResonanceSubscription Subscribe(Action<T> next, Action<Exception> error, Action completed)
        {
            return (ResonanceSubscription)Subscribe(new ResonanceObserver<T>(next, error, completed));
        }

        /// <summary>
        /// Called when a new response has been received.
        /// </summary>
        /// <param name="response">The response.</param>
        public void OnNext(T response)
        {
            FirstMessageArrived = true;
            LastResponseTime = DateTime.Now;

            foreach (var observer in _observers)
            {
                observer.OnNext(response);
            }
        }

        /// <summary>
        /// Called when a new response has been received.
        /// </summary>
        /// <param name="response">The response.</param>
        public void OnNext(object response)
        {
            OnNext((T)response);
        }

        /// <summary>
        /// Called when the response is marked as completed.
        /// </summary>
        public void OnCompleted()
        {
            FirstMessageArrived = true;
            IsCompleted = true;

            foreach (var observer in _observers)
            {
                observer.OnCompleted();
            }

            if (!_completionSource.Task.IsCompleted)
            {
                _completionSource.SetResult(true);
            }
        }

        /// <summary>
        /// Called when the response has returned with an error.
        /// </summary>
        /// <param name="exception">The exception.</param>
        public void OnError(Exception exception)
        {
            FirstMessageArrived = true;
            IsCompleted = true;

            foreach (var observer in _observers)
            {
                observer.OnError(exception);
            }

            if (!_completionSource.Task.IsCompleted)
            {
                _completionSource.SetException(exception);
            }
        }
    }
}
