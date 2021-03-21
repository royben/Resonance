using System.Collections.Concurrent;
using System.Threading;

namespace Resonance.Threading
{
    /// <summary>
    /// Represents a blocking concurrent queue for a producer consumer multi threading scenario.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <seealso cref="System.Collections.Concurrent.BlockingCollection{T}" />
    internal class ProducerConsumerQueue<T> : BlockingCollection<T>
    {
        /// <summary>
        /// Initializes a new instance of the ProducerConsumerQueue, Use Add and TryAdd for Enqueue and TryEnqueue and Take and TryTake for Dequeue and TryDequeue functionality
        /// </summary>
        public ProducerConsumerQueue()
            : base(new ConcurrentQueue<T>())
        {
        }

        /// <summary>
        /// Initializes a new instance of the ProducerConsumerQueue, Use Add and TryAdd for Enqueue and TryEnqueue and Take and TryTake for Dequeue and TryDequeue functionality
        /// </summary>
        /// <param name="maxSize"></param>
        public ProducerConsumerQueue(int maxSize)
            : base(new ConcurrentQueue<T>(), maxSize)
        {
        }

        /// <summary>
        /// Enqueues the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        public void BlockEnqueue(T item)
        {
            Add(item);
        }

        /// <summary>
        /// Blocks until an item is available for dequeuing.
        /// </summary>
        /// <returns></returns>
        public T BlockDequeue()
        {
            return Take();
        }

        /// <summary>
        /// Blocks until an item is available for dequeuing.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public T BlockDequeue(CancellationToken cancellationToken)
        {
            return Take(cancellationToken);
        }
    }
}
