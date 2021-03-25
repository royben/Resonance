using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance.Discovery
{
    /// <summary>
    /// Represents an <see cref="IResonanceDiscoveryClient{TDiscoveryInfo, TDecoder, TDiscoveredService}.ServiceDiscovered"/> event arguments.
    /// </summary>
    /// <typeparam name="TDiscoveredService">The type of the discovered service.</typeparam>
    /// <typeparam name="TDiscoveryInfo">The type of the discovery information.</typeparam>
    /// <seealso cref="System.EventArgs" />
    public class ResonanceDiscoveredServiceEventArgs<TDiscoveredService,TDiscoveryInfo> : EventArgs where TDiscoveredService : IResonanceDiscoveredService<TDiscoveryInfo>
    {
        /// <summary>
        /// Gets or sets the service information.
        /// </summary>
        public TDiscoveredService DiscoveredService { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResonanceDiscoveredServiceEventArgs{TDiscoveredService, TDiscoveryInfo}"/> class.
        /// </summary>
        /// <param name="discoveredService">The discovered service.</param>
        public ResonanceDiscoveredServiceEventArgs(TDiscoveredService discoveredService)
        {
            DiscoveredService = discoveredService;
        }
    }
}
