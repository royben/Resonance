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
    /// <seealso cref="Resonance.IResonanceTranscodingFactory" />
    public class ResonanceTranscodingFactory : IResonanceTranscodingFactory
    {
        private static readonly Lazy<ResonanceTranscodingFactory> _default = new Lazy<ResonanceTranscodingFactory>(() => new ResonanceTranscodingFactory());
        private readonly ConcurrentDictionary<String, IResonanceEncoder> _encoders;
        private readonly ConcurrentDictionary<String, IResonanceDecoder> _decoders;
        private static object _initLock = new object();

        /// <summary>
        /// Gets the default instance.
        /// </summary>
        public static ResonanceTranscodingFactory Default
        {
            get { return _default.Value; }
        }

        /// <summary>
        /// Prevents a default instance of the <see cref="ResonanceTranscodingFactory"/> class from being created.
        /// </summary>
        private ResonanceTranscodingFactory()
        {
            _encoders = new ConcurrentDictionary<string, IResonanceEncoder>();
            _decoders = new ConcurrentDictionary<string, IResonanceDecoder>();
        }

        /// <summary>
        /// Returns a decoder instance based on the transcoding name (e.g json, bson).
        /// </summary>
        /// <param name="name">The transcoding name.</param>
        /// <returns></returns>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException">Could not locate a decoder for '{name}'.</exception>
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

        /// <summary>
        /// Returns an encoder instance based on the transcoding name (e.g json, bson).
        /// </summary>
        /// <param name="name">The transcoding name.</param>
        /// <returns></returns>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException">Could not locate an encoder for '{name}'.</exception>
        public IResonanceEncoder GetEncoder(string name)
        {
            lock (_initLock)
            {
                IResonanceEncoder encoder = null;

                if (_encoders.TryGetValue(name, out encoder))
                {
                    return encoder;
                }

                Dictionary<String, IResonanceEncoder> encoders = new Dictionary<string, IResonanceEncoder>();

                foreach (var type in AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes()).Where(x => x.GetCustomAttribute<ResonanceTranscodingAttribute>() != null && typeof(ResonanceEncoder).IsAssignableFrom(x)))
                {
                    encoders.Add(type.GetCustomAttribute<ResonanceTranscodingAttribute>().Name, Activator.CreateInstance(type) as IResonanceEncoder);
                }

                foreach (var d in encoders)
                {
                    _encoders[d.Key] = d.Value;
                }

                if (encoders.TryGetValue(name, out encoder))
                {
                    return encoder;
                }

                throw new KeyNotFoundException($"Could not locate an encoder for '{name}'.");
            }
        }
    }
}
