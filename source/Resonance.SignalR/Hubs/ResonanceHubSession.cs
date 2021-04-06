using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.SignalR.Hubs
{
    /// <summary>
    /// Represents a Resonance SignalR session that is maintained by a SignalR hub.
    /// </summary>
    /// <typeparam name="TServiceInformation">The type of the service information.</typeparam>
    public class ResonanceHubSession<TServiceInformation> where TServiceInformation : IResonanceServiceInformation
    {
        /// <summary>
        /// Gets the unique session identifier.
        /// </summary>
        public String SessionId { get; }

        /// <summary>
        /// Gets or sets the registered service that hosts this session.
        /// </summary>
        public ResonanceHubRegisteredService<TServiceInformation> Service { get; set; }

        /// <summary>
        /// Gets or sets the connection id of the connecting adapter.
        /// </summary>
        public String ConnectedConnectionId { get; set; }

        /// <summary>
        /// Gets or sets the accepting adapter connection id.
        /// </summary>
        public String AcceptedConnectionId { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResonanceHubSession{TServiceInformation}"/> class.
        /// </summary>
        public ResonanceHubSession()
        {
            SessionId = Guid.NewGuid().ToString();
        }
    }
}
