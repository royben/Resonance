using Resonance.Adapters.InMemory;
using Resonance.Adapters.Tcp;
using Resonance.Adapters.Udp;
using Resonance.Transcoding.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using static Resonance.ResonanceTransporterBuilder;

namespace Resonance
{
    public interface IResonanceTransporterBuilder
    {
        /// <summary>
        /// Creates a new transporter builder instance.
        /// </summary>
        IAdapterBuilder Create();
    }

    public class ResonanceTransporterBuilder :
        IAdapterBuilder,
        IBuildTransporter,
        ITcpAdapertBuilder,
        IUdpAdapterBuilder,
        IUdpRemoteEndPointBuilder,
        ITcpAdapterPortBuilder,
        IInMemoryAdapterBuilder,
        ITranscodingBuilder,
        IKeepAliveBuilder,
        IEncryptionBuilder,
        ICompressionBuilder,
        IResonanceTransporterBuilder
    {
        #region Builders

        public interface IBuildTransporter
        {
            /// <summary>
            /// Returns the transporter instance.
            /// </summary>
            /// <returns></returns>
            IResonanceTransporter Build();
        }

        public interface IAdapterBuilder
        {
            /// <summary>
            /// Sets the transporter adapter to <see cref="TcpAdapter"/>.
            /// </summary>
            ITcpAdapertBuilder WithTcpAdapter();

            /// <summary>
            /// Sets the transporter adapter to <see cref="UdpAdapter"/>.
            /// </summary>
            IUdpAdapterBuilder WithUdpAdapter();

            /// <summary>
            /// Sets the transporter adapter to <see cref="InMemoryAdapter"/>.
            /// </summary>
            IInMemoryAdapterBuilder WithInMemoryAdapter();

            /// <summary>
            /// Sets the transporter adapter.
            /// </summary>
            ITranscodingBuilder WithAdapter(IResonanceAdapter adapter);
        }

        public interface ITcpAdapertBuilder
        {
            /// <summary>
            /// Sets the TCP adapter IP address.
            /// </summary>
            /// <param name="address">The address.</param>
            ITcpAdapterPortBuilder WithAddress(String address);
        }

        public interface ITcpAdapterPortBuilder
        {
            /// <summary>
            /// Sets the TCP adapter port.
            /// </summary>
            /// <param name="port">The port.</param>
            ITranscodingBuilder WithPort(int port);
        }

        public interface IUdpAdapterBuilder
        {
            /// <summary>
            /// Sets the UDP adapter local end point.
            /// </summary>
            /// <param name="endpoint">The endpoint.</param>
            IUdpRemoteEndPointBuilder WithLocalEndPoint(IPEndPoint endpoint);
        }

        public interface IUdpRemoteEndPointBuilder
        {
            /// <summary>
            /// Sets the UDP adapter remote end point.
            /// </summary>
            /// <param name="endpoint">The endpoint.</param>
            ITranscodingBuilder WithRemoteEndPoint(IPEndPoint endpoint);
        }

        public interface IInMemoryAdapterBuilder
        {
            /// <summary>
            /// Sets the In-Memory adapter address.
            /// </summary>
            /// <param name="address">The address.</param>
            ITranscodingBuilder WithAddress(String address);
        }

        public interface ITranscodingBuilder
        {
            /// <summary>
            /// Sets the transporter encoder/decoder.
            /// </summary>
            /// <typeparam name="TEncoder">The type of the encoder.</typeparam>
            /// <typeparam name="TDecoder">The type of the decoder.</typeparam>
            IKeepAliveBuilder WithTranscoding<TEncoder, TDecoder>() where TEncoder : IResonanceEncoder where TDecoder : IResonanceDecoder;

            /// <summary>
            /// Sets the transporter encoder/decoder.
            /// </summary>
            /// <typeparam name="TEncoder">The type of the encoder.</typeparam>
            /// <typeparam name="TDecoder">The type of the decoder.</typeparam>
            /// <param name="encoder">The encoder.</param>
            /// <param name="decoder">The decoder.</param>
            /// <returns></returns>
            IKeepAliveBuilder WithTranscoding<TEncoder, TDecoder>(TEncoder encoder, TDecoder decoder) where TEncoder : IResonanceEncoder where TDecoder : IResonanceDecoder;
        }

        public interface IKeepAliveBuilder : IBuildTransporter
        {
            /// <summary>
            /// Enables the transporter automatic keep alive mechanism.
            /// </summary>
            IEncryptionBuilder WithKeepAlive();

            /// <summary>
            /// Enables the transporter automatic keep alive mechanism.
            /// </summary>
            /// <param name="interval">Keep alive signal interval.</param>
            /// <param name="retries">Number of failed retries.</param>
            IEncryptionBuilder WithKeepAlive(TimeSpan interval, int retries);

            /// <summary>
            /// Disables the transporter automatic keep alive mechanism.
            /// </summary>
            IEncryptionBuilder NoKeepAlive();
        }

        public interface IEncryptionBuilder : IBuildTransporter
        {
            /// <summary>
            /// Specify that the transporter encoding/decoding should be encrypted.
            /// </summary>
            /// <param name="password">The encryption password.</param>
            ICompressionBuilder WithEncryption(String password);

            /// <summary>
            /// No encoding/decoding encryption.
            /// </summary>
            ICompressionBuilder NoEncryption();
        }

        public interface ICompressionBuilder : IBuildTransporter
        {
            /// <summary>
            /// Specify that the transporter encoding/decoding should be compressed.
            /// </summary>
            IBuildTransporter WithCompression();

            /// <summary>
            /// No encoding/decoding compression.
            /// </summary>
            IBuildTransporter NoCompression();
        }

        #endregion

        protected IResonanceTransporter Transporter { get; set; }

        private ResonanceTransporterBuilder()
        {
            Transporter = new ResonanceTransporter();
        }

        public ResonanceTransporterBuilder(IResonanceTransporter transporter) : this()
        {
            Transporter = transporter;
        }

        public static IResonanceTransporterBuilder New()
        {
            return new ResonanceTransporterBuilder();
        }

        public static IAdapterBuilder From(IResonanceTransporter transporter)
        {
            return new ResonanceTransporterBuilder(transporter);
        }

        public IAdapterBuilder Create()
        {
            return new ResonanceTransporterBuilder();
        }

        public ITcpAdapertBuilder WithTcpAdapter()
        {
            Transporter.Adapter = new TcpAdapter();
            return this;
        }

        public ITcpAdapterPortBuilder WithAddress(string address)
        {
            (Transporter.Adapter as TcpAdapter).Address = address;
            return this;
        }

        public ITranscodingBuilder WithPort(int port)
        {
            (Transporter.Adapter as TcpAdapter).Port = port;
            return this;
        }

        public IUdpAdapterBuilder WithUdpAdapter()
        {
            Transporter.Adapter = new UdpAdapter();
            return this;
        }

        public IUdpRemoteEndPointBuilder WithLocalEndPoint(IPEndPoint endpoint)
        {
            (Transporter.Adapter as UdpAdapter).LocalEndPoint = endpoint;
            return this;
        }

        public ITranscodingBuilder WithRemoteEndPoint(IPEndPoint endpoint)
        {
            (Transporter.Adapter as UdpAdapter).RemoteEndPoint = endpoint;
            return this;
        }

        public IInMemoryAdapterBuilder WithInMemoryAdapter()
        {
            return this;
        }

        ITranscodingBuilder IInMemoryAdapterBuilder.WithAddress(string address)
        {
            Transporter.Adapter = new InMemoryAdapter(address);
            return this;
        }

        public ITranscodingBuilder WithAdapter(IResonanceAdapter adapter)
        {
            Transporter.Adapter = adapter;
            return this;
        }

        public IKeepAliveBuilder WithTranscoding<TEncoder, TDecoder>()
            where TEncoder : IResonanceEncoder
            where TDecoder : IResonanceDecoder
        {
            return WithTranscoding(Activator.CreateInstance<TEncoder>(), Activator.CreateInstance<TDecoder>());
        }

        public IKeepAliveBuilder WithTranscoding<TEncoder, TDecoder>(TEncoder encoder, TDecoder decoder)
            where TEncoder : IResonanceEncoder
            where TDecoder : IResonanceDecoder
        {
            Transporter.Encoder = encoder;
            Transporter.Decoder = decoder;
            return this;
        }

        public IEncryptionBuilder WithKeepAlive()
        {
            Transporter.KeepAliveConfiguration.Enabled = true;
            Transporter.KeepAliveConfiguration.EnableAutoResponse = true;
            return this;
        }

        public IEncryptionBuilder WithKeepAlive(TimeSpan interval, int retries)
        {
            WithKeepAlive();
            Transporter.KeepAliveConfiguration.Interval = interval;
            Transporter.KeepAliveConfiguration.Retries = (uint)retries;
            return this;
        }

        public IEncryptionBuilder NoKeepAlive()
        {
            Transporter.KeepAliveConfiguration.Enabled = false;
            return this;
        }

        public ICompressionBuilder WithEncryption(string password)
        {
            Transporter.Encoder.EncryptionConfiguration.Enabled = true;
            Transporter.Encoder.EncryptionConfiguration.SetSymmetricAlgorithmPassword(password);
            Transporter.Decoder.EncryptionConfiguration.Enabled = true;
            Transporter.Decoder.EncryptionConfiguration.SetSymmetricAlgorithmPassword(password);
            return this;
        }

        public ICompressionBuilder NoEncryption()
        {
            Transporter.Encoder.EncryptionConfiguration.Enabled = false;
            Transporter.Decoder.EncryptionConfiguration.Enabled = false;
            return this;
        }

        public IBuildTransporter WithCompression()
        {
            Transporter.Encoder.CompressionConfiguration.Enabled = true;
            Transporter.Decoder.CompressionConfiguration.Enabled = true;
            return this;
        }

        public IBuildTransporter NoCompression()
        {
            Transporter.Encoder.CompressionConfiguration.Enabled = false;
            Transporter.Decoder.CompressionConfiguration.Enabled = false;
            return this;
        }

        public IResonanceTransporter Build()
        {
            return Transporter;
        }
    }
}
