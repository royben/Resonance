using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.Adapters.InMemory
{
    internal static class InMemoryAdaptersManager
    {
        private static List<InMemoryAdapter> _adapters;

        static InMemoryAdaptersManager()
        {
            _adapters = new List<InMemoryAdapter>();
        }

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

        internal static void UnregisterAdapter(InMemoryAdapter adapter)
        {
            if (adapter == null)
            {
                throw new NullReferenceException("Cannot disconnect null adapter.");
            }

            _adapters.Remove(adapter);
        }

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
