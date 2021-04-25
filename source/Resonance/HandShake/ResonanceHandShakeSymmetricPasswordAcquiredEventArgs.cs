using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance.HandShake
{
    /// <summary>
    /// Represents an <see cref="IResonanceHandShakeNegotiator.SymmetricPasswordAcquired"/> event arguments.
    /// </summary>
    /// <seealso cref="System.EventArgs" />
    public class ResonanceHandShakeSymmetricPasswordAcquiredEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the symmetric password.
        /// </summary>
        public String SymmetricPassword { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResonanceHandShakeSymmetricPasswordAcquiredEventArgs"/> class.
        /// </summary>
        /// <param name="symmetricPassword">The symmetric password.</param>
        public ResonanceHandShakeSymmetricPasswordAcquiredEventArgs(String symmetricPassword)
        {
            SymmetricPassword = symmetricPassword;
        }
    }
}
