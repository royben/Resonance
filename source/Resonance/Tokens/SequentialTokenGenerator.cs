using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Resonance.Tokens
{
    /// <summary>
    /// Represents an incremental number guid token generator.
    /// </summary>
    /// <seealso cref="Resonance.IResonanceTokenGenerator" />
    public class SequentialTokenGenerator : IResonanceTokenGenerator
    {
        private static long _token;

        /// <summary>
        /// Generates a message token.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns></returns>
        public string GenerateToken(object message)
        {
            long token = Interlocked.Increment(ref _token);
            return token.ToString();
        }
    }
}
