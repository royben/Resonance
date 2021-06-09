using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance
{
    /// <summary>
    /// Represents a resonance message.
    /// </summary>
    /// <seealso cref="Resonance.IResonanceMessage" />
    public class ResonanceMessage : IResonanceMessage
    {
        /// <summary>
        /// Gets or sets the message token.
        /// </summary>
        public String Token { get; set; }

        /// <summary>
        /// Gets or sets the message object.
        /// </summary>
        public object Object { get; set; }

        private String _objectTypeName;
        /// <summary>
        /// Gets the name of the message object type.
        /// </summary>
        [JsonIgnore]
        internal String ObjectTypeName
        {
            get
            {
                if (_objectTypeName == null)
                {
                    _objectTypeName = Object?.GetType().Name;
                }

                return _objectTypeName;
            }
        }

        /// <summary>
        /// Creates a generic ResonanceRequest T from the specified type.
        /// </summary>
        /// <param name="messageType">Type of the message.</param>
        /// <returns></returns>
        internal static ResonanceMessage CreateGenericMessage(Type messageType)
        {
            if (messageType != null)
            {
                Type[] typeArgs = { messageType };
                var genericType = typeof(ResonanceMessage<>).MakeGenericType(typeArgs);
                return Activator.CreateInstance(genericType) as ResonanceMessage;
            }
            else
            {
                return new ResonanceMessage();
            }
        }
    }

    /// <summary>
    /// Represents a resonance message.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <seealso cref="Resonance.IResonanceMessage" />
    public class ResonanceMessage<T> : ResonanceMessage
    {
        /// <summary>
        /// Gets or sets the message object.
        /// </summary>
        public new T Object
        {
            get { return (T)base.Object; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResonanceMessage{T}"/> class.
        /// </summary>
        public ResonanceMessage()
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResonanceMessage{T}"/> class.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <param name="token">The token.</param>
        public ResonanceMessage(T obj, String token)
        {
            base.Object = obj;
            Token = token;
        }
    }
}
