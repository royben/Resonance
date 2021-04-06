using Resonance.Adapters.InMemory;
using Resonance.Adapters.Tcp;
using Resonance.Adapters.Udp;
using Resonance.Transcoding.Bson;
using Resonance.Transcoding.Json;
using Resonance.Transcoding.Xml;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
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

            /// <summary>
            /// Initialize the TCP adapter from an existing <see cref="TcpClient"/>.
            /// </summary>
            /// <param name="tcpClient">The TCP client.</param>
            ITranscodingBuilder FromTcpClient(TcpClient tcpClient);
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

            /// <summary>
            /// Sets the transporter encoder/decoder to Json.
            /// </summary>
            IKeepAliveBuilder WithJsonTranscoding();

            /// <summary>
            /// Sets the transporter encoder/decoder to Bson.
            /// </summary>
            IKeepAliveBuilder WithBsonTranscoding();

            /// <summary>
            /// Sets the transporter encoder/decoder to XML.
            /// </summary>
            IKeepAliveBuilder WithXmlTranscoding();
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
            ICompressionBuilder WithEncryption();

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

        public ITranscodingBuilder FromTcpClient(TcpClient tcpClient)
        {
            Transporter.Adapter = new TcpAdapter(tcpClient);
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

        public IKeepAliveBuilder WithJsonTranscoding()
        {
            Transporter.Encoder = new JsonEncoder();
            Transporter.Decoder = new JsonDecoder();
            return this;
        }

        public IKeepAliveBuilder WithBsonTranscoding()
        {
            Transporter.Encoder = new BsonEncoder();
            Transporter.Decoder = new BsonDecoder();
            return this;
        }

        public IKeepAliveBuilder WithXmlTranscoding()
        {
            Transporter.Encoder = new XmlEncoder();
            Transporter.Decoder = new XmlDecoder();
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

        public ICompressionBuilder WithEncryption()
        {
            Transporter.CryptographyConfiguration.Enabled = true;
            return this;
        }

        public ICompressionBuilder NoEncryption()
        {
            Transporter.CryptographyConfiguration.Enabled = false;
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
