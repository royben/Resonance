using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance.Exceptions
{
    /// <summary>
    /// Represents a KeepAlive mechanism timeout exception.
    /// </summary>
    /// <seealso cref="System.TimeoutException" />
    public class ResonanceKeepAliveException : TimeoutException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ResonanceKeepAliveException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public ResonanceKeepAliveException(String message) : base(message)
        {

        }
    }
}
