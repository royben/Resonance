using Resonance.Threading;
using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance.Reactive
{
    public class ResonanceObservable<T> : IObservable<T>, IResonanceObservable
    {
        private readonly ConcurrentList<IObserver<T>> _observers = new ConcurrentList<IObserver<T>>();

        public bool IsCompleted { get; private set; }
        public bool FirstMessageArrived { get; private set; }
        public DateTime LastResponseTime { get; private set; }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            ResonanceSubscription subscription = new ResonanceSubscription(() => _observers.Remove(observer));
            _observers.Add(observer);
            return subscription;
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
        }

        public void OnError(Exception ex)
        {
            FirstMessageArrived = true;
            IsCompleted = true;

            foreach (var observer in _observers)
            {
                observer.OnError(ex);
            }
        }

        public void OnNext(object value)
        {
            OnNext((T)value);
        }
    }
}
