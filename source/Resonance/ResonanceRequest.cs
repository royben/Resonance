using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance
{
    /// <summary>
    /// Represents a resonance request.
    /// </summary>
    /// <seealso cref="Resonance.IResonanceMessage" />
    public class ResonanceRequest : IResonanceMessage
    {
        /// <summary>
        /// Gets or sets the request token.
        /// </summary>
        public String Token { get; set; }

        /// <summary>
        /// Gets or sets the request message.
        /// </summary>
        public object Message { get; set; }
    }

    /// <summary>
    /// Represents a resonance request.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <seealso cref="Resonance.IResonanceMessage" />
    public class ResonanceRequest<T> : ResonanceRequest
    {
        /// <summary>
        /// Gets or sets the request message.
        /// </summary>
        public new T Message
        {
            get { return (T)base.Message; }
        }
    }
}
