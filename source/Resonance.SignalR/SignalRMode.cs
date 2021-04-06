using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.SignalR
{
    /// <summary>
    /// Represents the two modes of operations for SignalR communication.
    /// </summary>
    public enum SignalRMode   
    {
        /// <summary>
        /// Using .NET Framework.
        /// </summary>
        Legacy,
        /// <summary>
        /// Using .NET Core.
        /// </summary>
        Core
    }
}
