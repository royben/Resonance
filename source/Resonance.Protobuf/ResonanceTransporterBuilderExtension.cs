using Resonance;
using Resonance.Protobuf.BuilderExtension;
using Resonance.Protobuf.Transcoding.Protobuf;
using System;
using System.Reflection;
using static Resonance.ResonanceTransporterBuilder;

public static class ResonanceTransporterBuilderExtension
{
    /// <summary>
    /// Sets the transporter encoding/decoding to Protobuf.
    /// </summary>
    /// <param name="adapterBuilder">The adapter builder.</param>
    public static ProtobufTranscodingBuilder WithProtobufTranscoding(this ITranscodingBuilder adapterBuilder)
    {
        adapterBuilder.WithTranscoding<ProtobufEncoder, ProtobufDecoder>();
        return new ProtobufTranscodingBuilder(adapterBuilder as ResonanceTransporterBuilder);
    }
}

namespace Resonance.Protobuf.BuilderExtension
{
    public class ProtobufTranscodingBuilderBase : IKeepAliveBuilder
    {
        protected ResonanceTransporterBuilder _builder;

        internal ProtobufTranscodingBuilderBase(ResonanceTransporterBuilder builder)
        {
            _builder = builder;
        }

        /// <summary>
        /// Returns the transporter instance.
        /// </summary>
        /// <returns></returns>
        public IResonanceTransporter Build()
        {
            return _builder.Build();
        }

        /// <summary>
        /// Disables the transporter automatic keep alive mechanism.
        /// </summary>
        /// <returns></returns>
        public IEncryptionBuilder NoKeepAlive()
        {
            return _builder.NoKeepAlive();
        }

        /// <summary>
        /// Enables the transporter automatic keep alive mechanism.
        /// </summary>
        /// <returns></returns>
        public IEncryptionBuilder WithKeepAlive()
        {
            return _builder.WithKeepAlive();
        }

        /// <summary>
        /// Enables the transporter automatic keep alive mechanism.
        /// </summary>
        /// <param name="interval">Keep alive signal interval.</param>
        /// <param name="retries">Number of failed retries.</param>
        /// <returns></returns>
        public IEncryptionBuilder WithKeepAlive(TimeSpan interval, int retries)
        {
            return _builder.WithKeepAlive(interval, retries);
        }
    }

    public class ProtobufTranscodingBuilder : ProtobufTranscodingBuilderBase
    {
        internal ProtobufTranscodingBuilder(ResonanceTransporterBuilder builder) : base(builder)
        {

        }

        /// <summary>
        /// Specifies the way message types names are encoded and decoded on the stream.
        /// </summary>
        /// <param name="method">The method.</param>
        /// <returns></returns>
        public ProtobufTranscodingMessageTypeHeaderMethod WithMessageTypeHeaderMethod(MessageTypeHeaderMethod method)
        {
            IResonanceTransporter transporter = _builder.GetType().GetProperty("Transporter", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_builder) as IResonanceTransporter;
            (transporter.Encoder as ProtobufEncoder).MessageTypeHeaderMethod = method;
            (transporter.Decoder as ProtobufDecoder).MessageTypeHeaderMethod = method;
            return new ProtobufTranscodingMessageTypeHeaderMethod(_builder);
        }
    }

    public class ProtobufTranscodingMessageTypeHeaderMethod : ProtobufTranscodingBuilderBase
    {
        internal ProtobufTranscodingMessageTypeHeaderMethod(ResonanceTransporterBuilder builder) : base(builder)
        {
            
        }

        /// <summary>
        /// Specifies the decoder protobuf type resolver.
        /// Must be set when <see cref="MessageTypeHeaderMethod"/> is set to <see cref="MessageTypeHeaderMethod.FullName"/> or <see cref="MessageTypeHeaderMethod.Name"/>.
        /// </summary>
        /// <typeparam name="T">Type of resolver.</typeparam>
        public IKeepAliveBuilder WithTypeResolver<T>() where T : IProtobufMessageTypeResolver, new()
        {
            return WithTypeResolver(Activator.CreateInstance<T>());
        }

        /// <summary>
        /// Specifies the decoder protobuf type resolver.
        /// Must be set when <see cref="MessageTypeHeaderMethod"/> is set to <see cref="MessageTypeHeaderMethod.FullName"/> or <see cref="MessageTypeHeaderMethod.Name"/>.
        /// </summary>
        /// <param name="resolver">The resolver.</param>
        /// <returns></returns>
        public IKeepAliveBuilder WithTypeResolver(IProtobufMessageTypeResolver resolver)
        {
            IResonanceTransporter transporter = _builder.GetType().GetProperty("Transporter", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_builder) as IResonanceTransporter;
            (transporter.Decoder as ProtobufDecoder).ProtobufTypeResolver = resolver;
            return _builder;
        }
    }
}
