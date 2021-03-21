using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance
{
    /// <summary>
    /// Represents a <see cref="ITransportComponent"/> state.
    /// </summary>
    public enum ResonanceComponentState
    {
        /// <summary>
        /// Disconnected.
        /// </summary>
        Disconnected,
        /// <summary>
        /// Started.
        /// </summary>
        Connected,
        /// <summary>
        /// Failed.
        /// </summary>
        Failed,
        /// <summary>
        /// Disposed.
        /// </summary>
        Disposed,
    }
}
