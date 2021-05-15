using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.SignalR.Hubs
{
    /// <summary>
    /// Represents a remote SignalR discovery client registration.
    /// </summary>
    /// <typeparam name="TCredentials">The type of the credentials.</typeparam>
    public class ResonanceHubDiscoveryClient<TCredentials>
    {
        /// <summary>
        /// Gets or sets the connection identifier.
        /// </summary>
        public String ConnectionId { get; set; }

        /// <summary>
        /// Gets or sets the credentials.
        /// </summary>
        public TCredentials Credentials { get; set; }
    }
}
