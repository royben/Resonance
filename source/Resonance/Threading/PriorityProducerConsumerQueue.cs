using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.Threading
{
    /// <summary>
    /// Priority queue.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class PriorityProducerConsumerQueue<T>
    {
        private readonly ProducerConsumerQueue<T> _lowPriorityCollection;
        private readonly ProducerConsumerQueue<T> _standardPriorityCollection;
        private readonly ProducerConsumerQueue<T> _highPriorityCollection;
        private readonly ProducerConsumerQueue<T>[] _collections;

        public PriorityProducerConsumerQueue()
        {
            _lowPriorityCollection = new ProducerConsumerQueue<T>();
            _standardPriorityCollection = new ProducerConsumerQueue<T>();
            _highPriorityCollection = new ProducerConsumerQueue<T>();
            _collections = new ProducerConsumerQueue<T>[] { _highPriorityCollection, _standardPriorityCollection, _lowPriorityCollection };
        }

        /// <summary>
        /// Enqueues the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="priority">Queue priority.</param>
        public void BlockEnqueue(T item, QueuePriority priority = QueuePriority.Standard)
        {
            switch (priority)
            {
                case QueuePriority.Low:
                    _lowPriorityCollection.Add(item);
                    break;
                case QueuePriority.Standard:
                    _standardPriorityCollection.Add(item);
                    break;
                case QueuePriority.High:
                    _highPriorityCollection.Add(item);
                    break;
            }
        }

        /// <summary>
        /// Blocks until an item is available for dequeuing.
        /// </summary>
        /// <returns></returns>
        public T BlockDequeue()
        {
            int index = BlockingCollection<T>.TakeFromAny(_collections, out T item);
            return item;
        }

        /// <summary>
        /// Gets total number of queued items.
        /// </summary>
        public int Count
        {
            get { return _standardPriorityCollection.Count + _lowPriorityCollection.Count + _highPriorityCollection.Count; }
        }

    }
}
