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
#if NET461
        /// <summary>
        /// Using .NET Framework.
        /// </summary>
        Legacy,
#endif
        /// <summary>
        /// Using .NET Core.
        /// </summary>
        Core
    }
}
