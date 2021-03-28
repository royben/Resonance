using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance.Protobuf.Transcoding.Protobuf
{
    /// <summary>
    /// Represents a custom protobuf message type resolver.
    /// </summary>
    public interface IProtobufMessageTypeResolver
    {
        /// <summary>
        /// Returns the type of the message by the specified message type name.
        /// </summary>
        /// <param name="typeName">Message type name.</param>
        /// <returns>The type of the message</returns>
        Type GetProtobufMessageType(String typeName);
    }
}