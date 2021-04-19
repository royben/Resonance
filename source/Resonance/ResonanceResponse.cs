using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance
{
    /// <summary>
    /// Represents a Resonance response message.
    /// </summary>
    /// <seealso cref="Resonance.IResonanceMessage" />
    public class ResonanceResponse : IResonanceMessage
    {
        /// <summary>
        /// Gets or sets the request token.
        /// </summary>
        public String Token { get; set; }

        /// <summary>
        /// Gets or sets the response message.
        /// </summary>
        public object Message { get; set; }

        private String _messageTypeName;
        /// <summary>
        /// Gets the name of the message type.
        /// </summary>
        [JsonIgnore]
        internal String MessageTypeName
        {
            get
            {
                if (_messageTypeName == null)
                {
                    _messageTypeName = Message?.GetType().Name;
                }

                return _messageTypeName;
            }
        }
    }

    /// <summary>
    /// Represents a Resonance response message.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <seealso cref="Resonance.IResonanceMessage" />
    public class ResonanceResponse<T> : ResonanceResponse
    {
        /// <summary>
        /// Gets or sets the response message.
        /// </summary>
        public new T Message
        {
            get { return (T)base.Message; }
        }
    }
}
