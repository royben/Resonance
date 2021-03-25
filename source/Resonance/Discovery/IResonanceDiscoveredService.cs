using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance.Discovery
{
    /// <summary>
    /// Represents a discovered service.
    /// </summary>
    public interface IResonanceDiscoveredService<TDiscoveryInfo>
    {
        /// <summary>
        /// Gets the discovered service information.
        /// </summary>
        TDiscoveryInfo DiscoveryInfo { get; }
    }
}
