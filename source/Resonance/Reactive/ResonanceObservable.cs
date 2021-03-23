using Resonance.Threading;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.Reactive
{
    public class ResonanceObservable<T> : IObservable<T>, IResonanceObservable
    {
        private readonly ConcurrentList<IObserver<T>> _observers = new ConcurrentList<IObserver<T>>();
        private TaskCompletionSource<object> _completionSource;


        public bool IsCompleted { get; private set; }
        public bool FirstMessageArrived { get; private set; }
        public DateTime LastResponseTime { get; private set; }

        public ResonanceObservable()
        {
            _completionSource = new TaskCompletionSource<object>();
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            ResonanceSubscription subscription = new ResonanceSubscription(() => _observers.Remove(observer), _completionSource);
            _observers.Add(observer);
            return subscription;
        }

        public ResonanceSubscription Subscribe(Action<T> next, Action<Exception> error, Action completed)
        {
            return (ResonanceSubscription)Subscribe(new ResonanceObserver<T>(next, error, completed));
        }

        public void OnNext(T value)
        {
            FirstMessageArrived = true;
            LastResponseTime = DateTime.Now;

            foreach (var observer in _observers)
            {
                observer.OnNext(value);
            }
        }

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

        public void OnError(Exception ex)
        {
            FirstMessageArrived = true;
            IsCompleted = true;

            foreach (var observer in _observers)
            {
                observer.OnError(ex);
            }

            if (!_completionSource.Task.IsCompleted)
            {
                _completionSource.SetException(ex);
            }
        }

        public void OnNext(object value)
        {
            OnNext((T)value);
        }
    }
}
