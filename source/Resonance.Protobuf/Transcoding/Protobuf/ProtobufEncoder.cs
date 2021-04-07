using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Google.Protobuf;

namespace Resonance.Protobuf.Transcoding.Protobuf
{
    /// <summary>
    /// Represents a Resonance protobuf message decoder.
    /// </summary>
    /// <seealso cref="Resonance.ResonanceEncoder" />
    [ResonanceTranscoding("proto")]
    public class ProtobufEncoder : ResonanceEncoder
    {
        /// <summary>
        /// Gets or sets way protobuf message types are encoded to the stream.
        /// </summary>
        public MessageTypeHeaderMethod MessageTypeHeaderMethod { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProtobufEncoder"/> class.
        /// </summary>
        public ProtobufEncoder()
        {
            MessageTypeHeaderMethod = MessageTypeHeaderMethod.AssemblyQualifiedName;
        }

        /// <summary>
        /// Encodes the specified message using the specified writer.
        /// </summary>
        /// <param name="writer">The binary writer.</param>
        /// <param name="message">The message.</param>
        /// <exception cref="InvalidOperationException">The specified message '{message.GetType().Name}' is not a valid protobuf message.</exception>
        protected override void Encode(BinaryWriter writer, object message)
        {
            if (!(message is IMessage))
            {
                throw new InvalidOperationException($"The specified message '{message.GetType().Name}' is not a valid protobuf message.");
            }

            switch (MessageTypeHeaderMethod)
            {
                case MessageTypeHeaderMethod.AssemblyQualifiedName:
                    writer.Write(message.GetType().AssemblyQualifiedName);
                    break;
                case MessageTypeHeaderMethod.FullName:
                    writer.Write(message.GetType().FullName);
                    break;
                case MessageTypeHeaderMethod.Name:
                    writer.Write(message.GetType().Name);
                    break;
            }

            writer.Write((message as IMessage).ToByteArray());
        }
    }
}
