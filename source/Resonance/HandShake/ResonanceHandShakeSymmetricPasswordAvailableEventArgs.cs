using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance.HandShake
{
    /// <summary>
    /// Represents an <see cref="IResonanceHandShakeNegotiator.SymmetricPasswordAvailable"/> event arguments.
    /// </summary>
    /// <seealso cref="System.EventArgs" />
    public class ResonanceHandShakeSymmetricPasswordAvailableEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the symmetric password.
        /// </summary>
        public String SymmetricPassword { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResonanceHandShakeSymmetricPasswordAvailableEventArgs"/> class.
        /// </summary>
        /// <param name="symmetricPassword">The symmetric password.</param>
        public ResonanceHandShakeSymmetricPasswordAvailableEventArgs(String symmetricPassword)
        {
            SymmetricPassword = symmetricPassword;
        }
    }
}
