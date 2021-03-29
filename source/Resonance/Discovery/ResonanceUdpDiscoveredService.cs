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
        /// Initializes a new instance of the <see cref="ResonanceUdpDiscoveredService{TDiscoveryInfo}"/> class.
        /// </summary>
        /// <param name="info">The information.</param>
        /// <param name="address">The service address.</param>
        public ResonanceUdpDiscoveredService(TDiscoveryInfo info, String address)
        {
            DiscoveryInfo = info;
            Address = address;
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
            return (other.Address == this.Address);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            int hashCode = 1893673367;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Address);
            return hashCode;
        }
    }
}
