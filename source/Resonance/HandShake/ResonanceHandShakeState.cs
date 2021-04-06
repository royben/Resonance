using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance.HandShake
{
    /// <summary>
    /// Represents a <see cref="IResonanceHandShakeNegotiator"/> state.
    /// </summary>
    public enum ResonanceHandShakeState
    {
        /// <summary>
        /// No action executed.
        /// </summary>
        Idle,
        /// <summary>
        /// Negotiation in progress.
        /// </summary>
        InProgress,
        /// <summary>
        /// Handshake completed.
        /// </summary>
        Completed,
    }
}
