using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.SignalR
{
    /// <summary>
    /// Represents a service information.
    /// </summary>
    public interface IResonanceServiceInformation
    {
        /// <summary>
        /// Gets or sets the service identifier.
        /// </summary>
        String ServiceId { get; set; }
    }
}
