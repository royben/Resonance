using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Resonance
{
    /// <summary>
    /// Represents a Resonance component capable of connecting and disconnecting.
    /// </summary>
    public interface IResonanceConnectionComponent
    {
        /// <summary>
        /// Connects this component.
        /// </summary>
        /// <returns></returns>
        Task Connect();

        /// <summary>
        /// Disconnects this component.
        /// </summary>
        /// <returns></returns>
        Task Disconnect();
    }
}
