using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Resonance
{
    public class ResonanceComponentCounterManager
    {
        private static object _lock = new object();
        private static Lazy<ResonanceComponentCounterManager> _default = new Lazy<ResonanceComponentCounterManager>(() => new ResonanceComponentCounterManager());

        public static ResonanceComponentCounterManager Default
        {
            get { return _default.Value; }
        }

        private ConcurrentDictionary<Type, int> _counters;

        private ResonanceComponentCounterManager()
        {
            _counters = new ConcurrentDictionary<Type, int>();
        }

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
    }
}
