using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Resonance
{
    /// <summary>
    /// Represents a Resonance component capable of connecting and disconnecting.
    /// </summary>
    public interface IResonanceConnectionComponent : IResonanceComponent
    {
        /// <summary>
        /// Connects this component.
        /// </summary>
        /// <returns></returns>
        Task ConnectAsync();

        /// <summary>
        /// Connects this component.
        /// </summary>
        /// <returns></returns>
        void Connect();

        /// <summary>
        /// Disconnects this component.
        /// </summary>
        /// <returns></returns>
        Task DisconnectAsync();

        /// <summary>
        /// Disconnects this component.
        /// </summary>
        /// <returns></returns>
        void Disconnect();
    }
}
