using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance.Discovery
{
    /// <summary>
    /// Represents a UDP discovered service.
    /// </summary>
    /// <typeparam name="TDiscoveryInfo">The type of the discovery information.</typeparam>
    /// <seealso cref="Resonance.Discovery.IResonanceDiscoveredService{TDiscoveryInfo}" />
    public class ResonanceUdpDiscoveredService<TDiscoveryInfo> : IResonanceDiscoveredService<TDiscoveryInfo>
    {
        /// <summary>
        /// Gets the discovered service information.
        /// </summary>
        public TDiscoveryInfo DiscoveryInfo { get; }

        /// <summary>
        /// Gets or sets the service IP address.
        /// </summary>
        public String Address { get; }

        /// <summary>
        /// Gets or sets the name of the host.
        /// </summary>
        public String HostName { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResonanceUdpDiscoveredService{TDiscoveryInfo}"/> class.
        /// </summary>
        /// <param name="info">The information.</param>
        /// <param name="address">The service address.</param>
        /// <param name="hostName">Name of the service host.</param>
        public ResonanceUdpDiscoveredService(TDiscoveryInfo info, String address, String hostName)
        {
            DiscoveryInfo = info;
            Address = address;
            HostName = hostName;
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object" />, is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            ResonanceUdpDiscoveredService<TDiscoveryInfo> other = obj as ResonanceUdpDiscoveredService<TDiscoveryInfo>;
            if (obj == null || this == null) return false;
            return (other.Address == this.Address && other.HostName == this.HostName);
        }

        public override int GetHashCode()
        {
            int hashCode = 1893673367;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Address);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(HostName);
            return hashCode;
        }
    }
}
