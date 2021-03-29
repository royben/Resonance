using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Resonance
{
    /// <summary>
    /// Represents a decoding factory capable of instantiating a decoder based on transcoding name.
    /// </summary>
    /// <seealso cref="Resonance.IResonanceDecodingFactory" />
    public class ResonanceDecodingFactory : IResonanceDecodingFactory
    {
        private static readonly Lazy<ResonanceDecodingFactory> _default = new Lazy<ResonanceDecodingFactory>(() => new ResonanceDecodingFactory());
        private readonly ConcurrentDictionary<String, IResonanceDecoder> _decoders;
        private static object _initLock = new object();

        /// <summary>
        /// Gets the default instance.
        /// </summary>
        public static ResonanceDecodingFactory Default
        {
            get { return _default.Value; }
        }

        /// <summary>
        /// Prevents a default instance of the <see cref="ResonanceDecodingFactory"/> class from being created.
        /// </summary>
        private ResonanceDecodingFactory()
        {
            _decoders = new ConcurrentDictionary<string, IResonanceDecoder>();
        }

        /// <summary>
        /// Returns decoder based on the transcoding name (e.g json, bson).
        /// </summary>
        /// <param name="name">The transcoding name.</param>
        /// <returns></returns>
        /// <exception cref="KeyNotFoundException">Could not locate a decoder for '{name}'.</exception>
        public IResonanceDecoder GetDecoder(string name)
        {
            lock (_initLock)
            {
                IResonanceDecoder decoder = null;

                if (_decoders.TryGetValue(name, out decoder))
                {
                    return decoder;
                }

                Dictionary<String, IResonanceDecoder> decoders = new Dictionary<string, IResonanceDecoder>();

                foreach (var type in AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes()).Where(x => x.GetCustomAttribute<ResonanceTranscodingAttribute>() != null && typeof(ResonanceDecoder).IsAssignableFrom(x)))
                {
                    decoders.Add(type.GetCustomAttribute<ResonanceTranscodingAttribute>().Name, Activator.CreateInstance(type) as IResonanceDecoder);
                }

                foreach (var d in decoders)
                {
                    _decoders[d.Key] = d.Value;
                }

                if (decoders.TryGetValue(name, out decoder))
                {
                    return decoder;
                }

                throw new KeyNotFoundException($"Could not locate a decoder for '{name}'.");
            }
        }
    }
}
