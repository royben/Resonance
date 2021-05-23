using Resonance;
using Resonance.Adapters.WebRTC;
using Resonance.USB.BuilderExtension;
using Resonance.WebRTC;
using Resonance.WebRTC.Messages;
using System;
using System.Collections.Generic;
using System.Reflection;
using static Resonance.ResonanceTransporterBuilder;

public static class ResonanceTransporterBuilderExtension
{
    /// <summary>
    /// Sets the transporter adapter to <see cref="WebRTCAdapter"/>.
    /// </summary>
    public static WebRTCAdapterBuilder WithWebRTCAdapter(this IAdapterBuilder adapterBuilder)
    {
        return new WebRTCAdapterBuilder(new WebRTCAdapterInfo() { builder = adapterBuilder as ResonanceTransporterBuilder });
    }
}

namespace Resonance.USB.BuilderExtension
{
    internal class WebRTCAdapterInfo
    {
        internal ResonanceTransporterBuilder builder;
        internal IResonanceTransporter signalingTransporter;
        internal WebRTCAdapter adapter;
        internal WebRTCAdapterRole role;
        internal WebRTCOfferRequest offerRequest;
        internal String offerRequestToken;
    }

    public class WebRTCAdapterBuilderBase
    {
        internal WebRTCAdapterInfo Info { get; set; }

        internal WebRTCAdapterBuilderBase(WebRTCAdapterInfo info)
        {
            Info = info;
        }
    }

    public class WebRTCAdapterBuilder : WebRTCAdapterBuilderBase
    {
        internal WebRTCAdapterBuilder(WebRTCAdapterInfo info) : base(info)
        {
        }

        /// <summary>
        /// Sets the adapter signaling transporter used to exchange session description and ice candidates.
        /// </summary>
        /// <param name="signalingTransporter">The signaling transporter.</param>
        /// <returns></returns>
        public WebRTCRoleBuilder WithSignalingTransporter(IResonanceTransporter signalingTransporter)
        {
            Info.signalingTransporter = signalingTransporter;
            return new WebRTCRoleBuilder(Info);
        }
    }

    public class WebRTCRoleBuilder : WebRTCAdapterBuilderBase
    {
        internal WebRTCRoleBuilder(WebRTCAdapterInfo info) : base(info)
        {
        }

        /// <summary>
        /// Sets the adapter role in the session.
        /// </summary>
        /// <param name="role">The role.</param>
        /// <returns></returns>
        public WebRTCIceServersBuilder WithRole(WebRTCAdapterRole role)
        {
            Info.role = role;
            SetAdapter();
            return new WebRTCIceServersBuilder(Info);
        }

        /// <summary>
        /// Initializes the adapter from an existing offer request.
        /// The adapter role with be <see cref="WebRTCAdapterRole.Accept"/> and response will be sent upon connection.
        /// </summary>
        /// <param name="offerRequest">The offer request.</param>
        /// <param name="requestToken">The offer request token.</param>
        /// <returns></returns>
        public WebRTCIceServersBuilder WithOfferRequest(WebRTCOfferRequest offerRequest, String requestToken)
        {
            Info.offerRequest = offerRequest;
            Info.offerRequestToken = requestToken;
            SetAdapter();
            return new WebRTCIceServersBuilder(Info);
        }

        /// <summary>
        /// Initializes the adapter from an existing offer request.
        /// The adapter role with be <see cref="WebRTCAdapterRole.Accept"/> and response will be sent upon connection.
        /// </summary>
        /// <param name="request">The offer request.</param>
        /// <returns></returns>
        public WebRTCIceServersBuilder WithOfferRequest(ResonanceMessage<WebRTCOfferRequest> request)
        {
            return WithOfferRequest(request.Object, request.Token);
        }

        private void SetAdapter()
        {
            IResonanceTransporter transporter = Info.builder.GetType().GetProperty("Transporter", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Info.builder) as IResonanceTransporter;

            if (Info.offerRequest != null)
            {
                transporter.Adapter = new WebRTCAdapter(Info.signalingTransporter, Info.offerRequest, Info.offerRequestToken);
            }
            else
            {
                transporter.Adapter = new WebRTCAdapter(Info.signalingTransporter, Info.role);
            }

            Info.adapter = transporter.Adapter as WebRTCAdapter;
        }
    }

    public class WebRTCIceServersBuilder : WebRTCAdapterBuilderBase, ITranscodingBuilder
    {
        internal WebRTCIceServersBuilder(WebRTCAdapterInfo info) : base(info)
        {

        }

        /// <summary>
        /// Clears and fills the adapter Ice Servers list with the default free, built-in servers.
        /// Use only for development/testing purpose, not production.
        /// </summary>
        public WebRTCIceServersBuilder WithDefaultIceServers()
        {
            Info.adapter.InitDefaultIceServers();
            return this;
        }

        /// <summary>
        /// Adds an ICE server.
        /// </summary>
        /// <param name="url">The server address.</param>
        /// <returns></returns>
        public WebRTCIceServersBuilder WithIceServer(String url)
        {
            Info.adapter.IceServers.Add(new WebRTCIceServer() { Url = url });
            return this;
        }

        /// <summary>
        /// Adds an authorized ICE server.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="userName">User name.</param>
        /// <param name="credentials">user credentials.</param>
        /// <returns></returns>
        public WebRTCIceServersBuilder WithIceServer(String url, String userName, String credentials)
        {
            Info.adapter.IceServers.Add(new WebRTCIceServer() { Url = url, UserName = userName, Credentials = credentials });
            return this;
        }

        /// <summary>
        /// Sets the transporter encoder/decoder to Bson.
        /// </summary>
        /// <returns></returns>
        public IKeepAliveBuilder WithBsonTranscoding()
        {
            return Info.builder.WithBsonTranscoding();
        }

        /// <summary>
        /// Sets the transporter encoder/decoder to Json.
        /// </summary>
        /// <returns></returns>
        public IKeepAliveBuilder WithJsonTranscoding()
        {
            return Info.builder.WithJsonTranscoding();
        }

        /// <summary>
        /// Sets the transporter encoder/decoder.
        /// </summary>
        /// <typeparam name="TEncoder">The type of the encoder.</typeparam>
        /// <typeparam name="TDecoder">The type of the decoder.</typeparam>
        /// <returns></returns>
        public IKeepAliveBuilder WithTranscoding<TEncoder, TDecoder>()
            where TEncoder : IResonanceEncoder
            where TDecoder : IResonanceDecoder
        {
            return Info.builder.WithTranscoding<TEncoder, TDecoder>();
        }

        /// <summary>
        /// Sets the transporter encoder/decoder.
        /// </summary>
        /// <typeparam name="TEncoder">The type of the encoder.</typeparam>
        /// <typeparam name="TDecoder">The type of the decoder.</typeparam>
        /// <param name="encoder">The encoder.</param>
        /// <param name="decoder">The decoder.</param>
        /// <returns></returns>
        public IKeepAliveBuilder WithTranscoding<TEncoder, TDecoder>(TEncoder encoder, TDecoder decoder)
            where TEncoder : IResonanceEncoder
            where TDecoder : IResonanceDecoder
        {
            return Info.builder.WithTranscoding<TEncoder, TDecoder>(encoder, decoder);
        }

        /// <summary>
        /// Sets the transporter encoder/decoder to XML.
        /// </summary>
        /// <returns></returns>
        public IKeepAliveBuilder WithXmlTranscoding()
        {
            return Info.builder.WithXmlTranscoding();
        }
    }
}
