using Resonance.Adapters.Tcp;
using Resonance.Transcoding.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance
{
    public class ResonanceTransporterBuilder
    {
        protected IResonanceTransporter _transporter;

        #region Transcoding

        public class TranscodingBuilder<TEncoder, TDecoder> : ResonanceTransporterBuilder where TEncoder : IResonanceEncoder, new() where TDecoder : IResonanceDecoder, new()
        {
            private IResonanceEncoder _encoder;
            private IResonanceDecoder _decoder;


            public TranscodingBuilder()
            {
                _encoder = Activator.CreateInstance<TEncoder>();
                _decoder = Activator.CreateInstance<TDecoder>();
                _transporter.Encoder = _encoder;
                _transporter.Decoder = _decoder;
            }

            public TranscodingBuilder<TEncoder, TDecoder> WithEncryption(String password)
            {
                _encoder.EncryptionConfiguration.Enabled = true;
                _decoder.EncryptionConfiguration.Enabled = true;
                _encoder.EncryptionConfiguration.SetSymmetricAlgorithmPassword(password);
                _decoder.EncryptionConfiguration.SetSymmetricAlgorithmPassword(password);
                return this;
            }

            public TranscodingBuilder<TEncoder, TDecoder> WithCompression()
            {
                _encoder.CompressionConfiguration.Enabled = true;
                _decoder.CompressionConfiguration.Enabled = true;
                return this;
            }
        }

        #endregion

        #region TcpAdapter

        public class TcpAdapterBuilder : ResonanceTransporterBuilder
        {
            private TcpAdapter _adapter;

            public TcpAdapterBuilder()
            {
                _adapter = new TcpAdapter();
            }

            public TcpAdapterBuilder WithAddress(String address)
            {
                _adapter.Address = address;
                return this;
            }

            public TcpAdapterBuilder WithPort(int port)
            {
                _adapter.Port = port;
                return this;
            }
        }

        #endregion

        public ResonanceTransporterBuilder()
        {
            _transporter = new ResonanceTransporter();
        }

        public TranscodingBuilder<TEncoder, TDecoder> WithTranscoding<TEncoder, TDecoder>() where TEncoder : IResonanceEncoder, new() where TDecoder : IResonanceDecoder, new()
        {
            return new TranscodingBuilder<TEncoder, TDecoder>();
        }

        public TcpAdapterBuilder WithTcpAdapter()
        {
            return new TcpAdapterBuilder();
        }

        public IResonanceTransporter Build()
        {
            return _transporter;
        }
    }
}
