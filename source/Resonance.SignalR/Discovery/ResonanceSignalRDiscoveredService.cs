using Resonance.Discovery;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.SignalR.Discovery
{
    /// <summary>
    /// Represents a <see cref="ResonanceSignalRDiscoveryClient{TReportedServiceInformation, TCredentials}"/> discovered service.
    /// </summary>
    /// <typeparam name="TDiscoveryInfo">The type of the discovery information.</typeparam>
    /// <seealso cref="Resonance.Discovery.IResonanceDiscoveredService{TDiscoveryInfo}" />
    public class ResonanceSignalRDiscoveredService<TDiscoveryInfo> : IResonanceDiscoveredService<TDiscoveryInfo>
    {
        /// <summary>
        /// Gets the discovered service information.
        /// </summary>
        public TDiscoveryInfo DiscoveryInfo { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResonanceSignalRDiscoveredService{TDiscoveryInfo}"/> class.
        /// </summary>
        /// <param name="discoveryInfo">The discovery information.</param>
        public ResonanceSignalRDiscoveredService(TDiscoveryInfo discoveryInfo)
        {
            DiscoveryInfo = discoveryInfo;
        }
    }
}
