using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance.Protobuf.Transcoding.Protobuf
{
    /// <summary>
    /// Represents the protobuf transcoding message type header method.
    /// </summary>
    public enum MessageTypeHeaderMethod
    {
        /// <summary>
        /// Type name with namespace and assembly.
        /// </summary>
        AssemblyQualifiedName,
        /// <summary>
        /// Type name with namespace. (<see cref="ProtobufDecoder.ProtobufTypeResolver"/> must be specified on the decoder.)
        /// </summary>
        FullName,
        /// <summary>
        /// Simple type name. (<see cref="ProtobufDecoder.ProtobufTypeResolver"/> must be specified on the decoder.)
        /// </summary>
        Name
    }
}
