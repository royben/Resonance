using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance
{
    /// <summary>
    /// Represents a Resonance communication message.
    /// </summary>
    public interface IResonanceMessage
    {
        /// <summary>
        /// Gets or sets the message token.
        /// </summary>
        String Token { get; set; }

        /// <summary>
        /// Gets or sets the message.
        /// </summary>
        Object Message { get; set; }
    }
}
