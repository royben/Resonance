using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance.Tokens
{
    /// <summary>
    /// Generates tokens based on the message GetHashCode method.
    /// See <see cref="Object.GetHashCode()"/>.
    /// </summary>
    /// <seealso cref="Resonance.IResonanceTokenGenerator" />
    public class HashCodeTokenGenerator : IResonanceTokenGenerator
    {
        /// <summary>
        /// Generates a message token.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns></returns>
        public string GenerateToken(object message)
        {
            return message.GetHashCode().ToString();
        }
    }
}
