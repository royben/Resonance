using Resonance.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.Adapters.InMemory
{
    /// <summary>
    /// Represents the In-Memory adapters manager.
    /// </summary>
    internal static class InMemoryAdaptersManager
    {
        private static readonly ConcurrentList<InMemoryAdapter> _adapters;

        /// <summary>
        /// Initializes the <see cref="InMemoryAdaptersManager"/> class.
        /// </summary>
        static InMemoryAdaptersManager()
        {
            _adapters = new ConcurrentList<InMemoryAdapter>();
        }

        /// <summary>
        /// Registers the specified adapter.
        /// </summary>
        /// <param name="adapter">The adapter.</param>
        /// <exception cref="System.NullReferenceException">Cannot connect null adapter.</exception>
        /// <exception cref="System.InvalidOperationException">
        /// Cannot register in-memory adapter with null address.
        /// or
        /// Cannot register more than two in-memory adapters with the same address.
        /// or
        /// The specified in-memory adapter is already registered.
        /// </exception>
        internal static void RegisterAdapter(InMemoryAdapter adapter)
        {
            if (adapter == null)
            {
                throw new NullReferenceException("Cannot connect null adapter.");
            }

            if (String.IsNullOrWhiteSpace(adapter.Address))
            {
                throw new InvalidOperationException("Cannot register in-memory adapter with null address.");
            }

            if (_adapters.Where(x => x.Address == adapter.Address).Count() > 1)
            {
                throw new InvalidOperationException("Cannot register more than two in-memory adapters with the same address.");
            }

            if (_adapters.Contains(adapter))
            {
                throw new InvalidOperationException("The specified in-memory adapter is already registered.");
            }

            _adapters.Add(adapter);
        }

        /// <summary>
        /// Unregisters the specified adapter.
        /// </summary>
        /// <param name="adapter">The adapter.</param>
        /// <exception cref="System.NullReferenceException">Cannot disconnect null adapter.</exception>
        internal static void UnregisterAdapter(InMemoryAdapter adapter)
        {
            if (adapter == null)
            {
                throw new NullReferenceException("Cannot disconnect null adapter.");
            }

            _adapters.Remove(adapter);
        }

        /// <summary>
        /// Writes the specified data to a matching adapter by the specified adapter address.
        /// </summary>
        /// <param name="adapter">The adapter.</param>
        /// <param name="data">The data.</param>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException"></exception>
        internal static void Write(InMemoryAdapter adapter, byte[] data)
        {
            var other_adapter = _adapters.ToList().FirstOrDefault(x => x.Address == adapter.Address && x != adapter);

            if (other_adapter != null)
            {
                other_adapter.EmulateDataAvailable(data);
            }
            else
            {
                throw new KeyNotFoundException($"No other in-memory adapter found with a matching address '{adapter.Address}'.");
            }
        }
    }
}
