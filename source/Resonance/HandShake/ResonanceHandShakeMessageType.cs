using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance.HandShake
{
    /// <summary>
    /// Represents a <see cref="ResonanceHandShakeMessage"/> type.
    /// </summary>
    public enum ResonanceHandShakeMessageType
    {
        /// <summary>
        /// handshake request.
        /// </summary>
        Request,
        /// <summary>
        /// Handshake response.
        /// </summary>
        Response,
        /// <summary>
        /// Handshake completed.
        /// </summary>
        Complete
    }
}
