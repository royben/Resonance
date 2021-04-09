using Resonance.Adapters.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.SignalR.Services
{
    /// <summary>
    /// Represents a SignalR service ConnectionRequest event arguments.
    /// </summary>
    /// <typeparam name="TCredentials">The type of the credentials.</typeparam>
    /// <typeparam name="TAdapterInformation">The type of the adapter information.</typeparam>
    /// <seealso cref="System.EventArgs" />
    public class ConnectionRequestEventArgs<TCredentials, TAdapterInformation> : EventArgs
    {
        private Func<String, SignalRAdapter<TCredentials>> _accept;
        private Action<String> _decline;

        /// <summary>
        /// Gets or sets the remote session identifier.
        /// </summary>
        public String SessionId { get; set; }

        /// <summary>
        /// Gets or sets the requesting adapter information.
        /// </summary>
        public TAdapterInformation RemoteAdapterInformation { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionRequestEventArgs{TCredentials, TAdapterInformation}"/> class.
        /// </summary>
        /// <param name="accept">The accept callback.</param>
        /// <param name="decline">The decline callback.</param>
        public ConnectionRequestEventArgs(Func<String, SignalRAdapter<TCredentials>> accept, Action<String> decline)
        {
            _accept = accept;
            _decline = decline;
        }

        /// <summary>
        /// Accepts the SignalR connection and return an adapter.
        /// </summary>
        /// <returns></returns>
        public SignalRAdapter<TCredentials> Accept()
        {
            return _accept?.Invoke(SessionId);
        }

        /// <summary>
        /// Declines the SignalR connection.
        /// </summary>
        /// <returns></returns>
        public void Decline()
        {
            _decline?.Invoke(SessionId);
        }
    }
}
