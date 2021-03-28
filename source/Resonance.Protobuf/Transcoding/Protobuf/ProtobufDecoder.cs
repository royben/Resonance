using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace Resonance.Protobuf.Transcoding.Protobuf
{
    /// <summary>
    /// Represents a Resonance protobuf message encoder.
    /// </summary>
    /// <seealso cref="Resonance.ResonanceDecoder" />
    public class ProtobufDecoder : ResonanceDecoder
    {
        private static Dictionary<Type, MessageParser> _parsers;

        /// <summary>
        /// Initializes the <see cref="ProtobufDecoder"/> class.
        /// </summary>
        static ProtobufDecoder()
        {
            _parsers = new Dictionary<Type, MessageParser>();
        }

        /// <summary>
        /// Gets or sets the protobuf message type resolver.
        /// Must be implemented when <see cref="MessageTypeHeaderMethod"/> is set to <see cref="MessageTypeHeaderMethod.FullName"/> or <see cref="MessageTypeHeaderMethod.Name"/>.
        /// </summary>
        public IProtobufMessageTypeResolver ProtobufTypeResolver { get; set; }

        /// <summary>
        /// Gets or sets way protobuf message types are decoded from the stream.
        /// </summary>
        public MessageTypeHeaderMethod MessageTypeHeaderMethod { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProtobufDecoder"/> class.
        /// </summary>
        public ProtobufDecoder()
        {
            MessageTypeHeaderMethod = MessageTypeHeaderMethod.AssemblyQualifiedName;
        }

        /// <summary>
        /// Decodes a message from the specified memory stream.
        /// </summary>
        /// <param name="stream">The memory stream.</param>
        /// <returns></returns>
        /// <exception cref="NullReferenceException"></exception>
        protected override object Decode(MemoryStream stream)
        {
            using (BinaryReader reader = new BinaryReader(stream))
            {
                String typeName = typeName = reader.ReadString();

                if (MessageTypeHeaderMethod == MessageTypeHeaderMethod.AssemblyQualifiedName)
                {
                    return GetParser(Type.GetType(typeName)).ParseFrom(stream);
                }
                else
                {
                    if (ProtobufTypeResolver == null) throw new NullReferenceException($"{nameof(ProtobufTypeResolver)} must be set when {nameof(MessageTypeHeaderMethod)} is set to '{MessageTypeHeaderMethod}'.");

                    return GetParser(ProtobufTypeResolver.GetProtobufMessageType(typeName)).ParseFrom(stream);
                }
            }
        }

        /// <summary>
        /// Gets the protobuf message type parser by the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        private MessageParser GetParser(Type type)
        {
            if (_parsers.ContainsKey(type))
            {
                return _parsers[type];
            }

            MessageParser parser = type.GetProperty("Parser", BindingFlags.Public | BindingFlags.Static).GetValue(null, null) as MessageParser;
            _parsers[type] = parser;

            return parser;
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        public override void Dispose()
        {

        }
    }
}
