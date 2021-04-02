using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.Adapters.SignalR
{
    /// <summary>
    /// Represents a Resonance SignalR adapter.
    /// </summary>
    /// <seealso cref="Resonance.IResonanceAdapter" />
    public interface ISignalRAdapter<TCredentials> : IResonanceAdapter
    {
        /// <summary>
        /// Gets the URL of the SignalR service.
        /// </summary>
        String Url { get; }

        /// <summary>
        /// Gets the service identifier.
        /// </summary>
        String ServiceId { get; }

        /// <summary>
        /// Gets the remote session identifier.
        /// </summary>
        String SessionId { get; }

        /// <summary>
        /// Gets or sets the adapter connection timeout.
        /// </summary>
        TimeSpan ConnectionTimeout { get; set; }

        /// <summary>
        /// Gets the adapter mode.
        /// </summary>
        SignalRAdapterRole Role { get; }
    }
}
