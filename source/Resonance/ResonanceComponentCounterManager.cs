using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Resonance
{
    /// <summary>
    /// Represents a Resonance component counter manager for providing automatic incremental counters for each type.
    /// </summary>
    public class ResonanceComponentCounterManager
    {
        private static object _lock = new object();
        private static Lazy<ResonanceComponentCounterManager> _default = new Lazy<ResonanceComponentCounterManager>(() => new ResonanceComponentCounterManager());

        /// <summary>
        /// Gets the default instance.
        /// </summary>
        public static ResonanceComponentCounterManager Default
        {
            get { return _default.Value; }
        }

        private ConcurrentDictionary<Type, int> _counters;

        private ResonanceComponentCounterManager()
        {
            _counters = new ConcurrentDictionary<Type, int>();
        }

        /// <summary>
        /// Increments the last counter to the specified type and returns the new count.
        /// </summary>
        /// <param name="component">The component.</param>
        /// <returns></returns>
        public int GetIncrement(IResonanceComponent component)
        {
            lock (_lock)
            {
                int counter = 0;
                Type type = component.GetType();

                if (_counters.TryGetValue(type, out counter))
                {
                    return ++_counters[type];
                }
                else
                {
                    _counters[type] = 1;
                    return 1;
                }
            }
        }

        /// <summary>
        /// Resets the counter for all types.
        /// </summary>
        public void Reset()
        {
            _counters = new ConcurrentDictionary<Type, int>();
        }
    }
}
