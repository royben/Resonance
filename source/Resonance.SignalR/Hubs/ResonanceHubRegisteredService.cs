using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.SignalR.Hubs
{
    /// <summary>
    /// Represents a remote SignalR service registration on a hub.
    /// </summary>
    /// <typeparam name="TServiceInformation">The type of the service information.</typeparam>
    public class ResonanceHubRegisteredService<TServiceInformation> where TServiceInformation : IResonanceServiceInformation
    {
        /// <summary>
        /// Gets or sets the connection identifier of the registering client.
        /// </summary>
        public String ConnectionId { get; set; }

        /// <summary>
        /// Gets or sets the service information.
        /// </summary>
        public TServiceInformation ServiceInformation { get; set; }
    }
}
