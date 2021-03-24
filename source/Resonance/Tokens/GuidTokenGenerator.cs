using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance.Tokens
{
    /// <summary>
    /// Represents a basic <see cref="Guid"/> token generator.
    /// </summary>
    /// <seealso cref="Resonance.IResonanceTokenGenerator" />
    public class GuidTokenGenerator : IResonanceTokenGenerator
    {
        /// <summary>
        /// Generates a message token.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns></returns>
        public string GenerateToken(object message)
        {
            return Guid.NewGuid().ToString();
        }
    }
}
