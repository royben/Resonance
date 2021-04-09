using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.Discovery
{
    /// <summary>
    /// Represents a Resonance discovery client capable of detecting and fetching information aboute a remote service.
    /// </summary>
    /// <typeparam name="TDiscoveryInfo">The type of the discovery information.</typeparam>
    /// <typeparam name="TDecoder">The type of the decoder.</typeparam>
    /// <typeparam name="TDiscoveredService">The type of the discovered service.</typeparam>
    /// <seealso cref="System.IDisposable" />
    public interface IResonanceDiscoveryClient<TDiscoveryInfo, TDecoder, TDiscoveredService> : IDisposable, IResonanceAsyncDisposable where TDiscoveryInfo : class, new() where TDecoder : IResonanceDecoder, new() where TDiscoveredService : IResonanceDiscoveredService<TDiscoveryInfo>
    {
        /// <summary>
        /// Occurs when a matching service has been discovered.
        /// </summary>
        event EventHandler<ResonanceDiscoveredServiceEventArgs<TDiscoveredService, TDiscoveryInfo>> ServiceDiscovered;

        /// <summary>
        /// Occurs when a discovered service is no longer responding.
        /// </summary>
        event EventHandler<ResonanceDiscoveredServiceEventArgs<TDiscoveredService, TDiscoveryInfo>> ServiceLost;

        /// <summary>
        /// Gets the decoder used to decode the service information message.
        /// </summary>
        TDecoder Decoder { get; }

        /// <summary>
        /// Gets a value indicating whether this client has started.
        /// </summary>
        bool IsStarted { get; }

        /// <summary>
        /// Start discovering.
        /// </summary>
        Task Start();


        /// <summary>
        /// Stop discovering.
        /// </summary>
        Task Stop();

        /// <summary>
        /// Asynchronous method for collecting discovered services within the given duration.
        /// </summary>
        /// <param name="maxDuration">The maximum duration to perform the scan.</param>
        /// <param name="maxServices">Drop the scanning after the maximum services discovered.</param>
        /// <returns></returns>
        Task<List<TDiscoveredService>> Discover(TimeSpan maxDuration, int? maxServices = null);
    }
}
