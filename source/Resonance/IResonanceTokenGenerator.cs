using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance
{
    /// <summary>
    /// Represents a message token generator.
    /// </summary>
    public interface IResonanceTokenGenerator
    {
        /// <summary>
        /// Generates a message token.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns></returns>
        String GenerateToken(Object message);
    }
}
