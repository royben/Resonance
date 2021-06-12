using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Resonance.Reactive;
using static Resonance.ResonanceTransporterBuilder;
using Resonance.HandShake;
using Resonance.RPC;

namespace Resonance
{
    public delegate void MessageHandlerDelegate<Message>(ResonanceMessage<Message> message) where Message : class;

    public delegate Task AsyncMessageHandlerDelegate<Message>(ResonanceMessage<Message> message) where Message : class;

    public delegate void MessageWithTransporterHandlerDelegate<Message>(IResonanceTransporter transporter, ResonanceMessage<Message> message) where Message : class;

    public delegate Task AsyncMessageWithTransporterHandlerDelegate<Message>(IResonanceTransporter transporter, ResonanceMessage<Message> message) where Message : class;

    public delegate ResonanceActionResult<Response> RequestHandlerDelegate<Request, Response>(Request request) where Request : class where Response : class;

    public delegate Task<ResonanceActionResult<Response>> AsyncRequestHandlerDelegate<Request, Response>(Request request) where Request : class where Response : class;

    public delegate ResonanceActionResult<Response> RequestWithTransporterHandlerDelegate<Request, Response>(IResonanceTransporter transporter, Request request) where Request : class where Response : class;

    public delegate Task<ResonanceActionResult<Response>> AsyncRequestWithTransporterHandlerDelegate<Request, Response>(IResonanceTransporter transporter, Request request) where Request : class where Response : class;

    /// <summary>
    /// Represents a Resonance Transporter capable of sending and receiving request/response messages.
    /// </summary>
    /// <seealso cref="Resonance.IResonanceComponent" />
    /// <seealso cref="Resonance.IResonanceStateComponent" />
    /// <seealso cref="Resonance.IResonanceConnectionComponent" />
    public interface IResonanceTransporter : IResonanceComponent, IResonanceStateComponent, IResonanceConnectionComponent, IDisposable, IResonanceAsyncDisposable
    {
        /// <summary>
        /// Occurs when a new message has been received.
        /// </summary>
        event EventHandler<ResonanceMessageReceivedEventArgs> MessageReceived;

        /// <summary>
        /// Occurs when a message has been sent.
        /// </summary>
        event EventHandler<ResonanceMessageEventArgs> MessageSent;

        /// <summary>
        /// Occurs when a sent message has failed.
        /// </summary>
        event EventHandler<ResonanceMessageFailedEventArgs> MessageFailed;

        /// <summary>
        /// Occurs when a new request message has been received.
        /// </summary>
        event EventHandler<ResonanceMessageReceivedEventArgs> RequestReceived;

        /// <summary>
        /// Occurs when a request has been sent.
        /// </summary>
        event EventHandler<ResonanceMessageEventArgs> RequestSent;

        /// <summary>
        /// Occurs when a request has failed.
        /// </summary>
        event EventHandler<ResonanceMessageFailedEventArgs> RequestFailed;

        /// <summary>
        /// Occurs when a request response has been received.
        /// </summary>
        event EventHandler<ResonanceMessageEventArgs> ResponseReceived;

        /// <summary>
        /// Occurs when a response has been sent.
        /// </summary>
        event EventHandler<ResonanceMessageEventArgs> ResponseSent;

        /// <summary>
        /// Occurs when a response has failed to be sent.
        /// </summary>
        event EventHandler<ResonanceMessageFailedEventArgs> ResponseFailed;

        /// <summary>
        /// Occurs when the keep alive mechanism is enabled and has failed by reaching the given timeout and retries.
        /// </summary>
        event EventHandler KeepAliveFailed;

        /// <summary>
        /// Occurs when the Transporter has lost the connection either because the connection has failed, adapter failed 
        /// or the remote peer has disconnected and Disconnect request was received.
        /// </summary>
        event EventHandler<ResonanceConnectionLostEventArgs> ConnectionLost;

        /// <summary>
        /// Occurs after incoming data has been decoded.
        /// Can be used to route data to other components or prevent further processing of the data by the transporter.
        /// </summary>
        event EventHandler<ResonancePreviewDecodingInfoEventArgs> PreviewDecodingInformation;

        /// <summary>
        /// Gets or sets the Resonance adapter used to send and receive actual encoded data.
        /// </summary>
        IResonanceAdapter Adapter { get; set; }

        /// <summary>
        /// Gets or sets the encoder to use for encoding outgoing messages.
        /// </summary>
        IResonanceEncoder Encoder { get; set; }

        /// <summary>
        /// Gets or sets the decoder to use for decoding incoming messages.
        /// </summary>
        IResonanceDecoder Decoder { get; set; }

        /// <summary>
        /// Gets or sets the message token generator.
        /// </summary>
        IResonanceTokenGenerator TokenGenerator { get; set; }

        /// <summary>
        /// Gets or sets the default request timeout.
        /// </summary>
        TimeSpan DefaultRequestTimeout { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to send a disconnection notification to the other side when disconnecting.
        /// </summary>
        bool NotifyOnDisconnect { get; set; }

        /// <summary>
        /// Gets or sets the keep alive configuration.
        /// </summary>
        ResonanceKeepAliveConfiguration KeepAliveConfiguration { get; }

        /// <summary>
        /// Gets the cryptography configuration.
        /// </summary>
        ResonanceCryptographyConfiguration CryptographyConfiguration { get; }

        /// <summary>
        /// Gets or sets the hand shake negotiator.
        /// </summary>
        IResonanceHandShakeNegotiator HandShakeNegotiator { get; set; }

        /// <summary>
        /// Gets or sets the message acknowledgment behavior when receiving and sending standard messages.
        /// </summary>
        ResonanceMessageAckBehavior MessageAcknowledgmentBehavior { get; set; }

        /// <summary>
        /// Returns true if communication is currently encrypted.
        /// </summary>
        bool IsChannelSecure { get; }

        /// <summary>
        /// Gets or sets a value indicating whether the transporter will get in to a failed state if the <see cref="Adapter"/> fails.
        /// </summary>
        bool FailsWithAdapter { get; set; }

        /// <summary>
        /// Gets the total number of queued outgoing messages.
        /// </summary>
        int OutgoingQueueCount { get; }

        /// <summary>
        /// Gets the number of current pending outgoing messages.
        /// </summary>
        int TotalPendingOutgoingMessages { get; }

        /// <summary>
        /// Gets the total of incoming messages.
        /// </summary>
        int TotalIncomingMessages { get; }

        /// <summary>
        /// Gets the total of outgoing messages.
        /// </summary>
        int TotalOutgoingMessages { get; }

        /// <summary>
        /// Registers a custom message handler.
        /// </summary>
        /// <typeparam name="Message">The type of the message.</typeparam>
        /// <param name="callback">The callback method to register.</param>
        IDisposable RegisterMessageHandler<Message>(MessageHandlerDelegate<Message> callback) where Message : class;

        /// <summary>
        /// Unregisters a custom message handler.
        /// </summary>
        /// <typeparam name="Message">The type of the message.</typeparam>
        /// <param name="callback">The callback method to detach.</param>
        void UnregisterMessageHandler<Message>(MessageHandlerDelegate<Message> callback) where Message : class;

        /// <summary>
        /// Registers a custom message handler.
        /// </summary>
        /// <typeparam name="Message">The type of the message.</typeparam>
        /// <param name="callback">The callback method to register.</param>
        IDisposable RegisterMessageHandler<Message>(AsyncMessageHandlerDelegate<Message> callback) where Message : class;

        /// <summary>
        /// Unregisters a custom message handler.
        /// </summary>
        /// <typeparam name="Message">The type of the message.</typeparam>
        /// <param name="callback">The callback method to detach.</param>
        void UnregisterMessageHandler<Message>(AsyncMessageHandlerDelegate<Message> callback) where Message : class;

        /// <summary>
        /// Registers a custom message handler.
        /// </summary>
        /// <typeparam name="Message">The type of the message.</typeparam>
        /// <param name="callback">The callback method to register.</param>
        IDisposable RegisterMessageHandler<Message>(MessageWithTransporterHandlerDelegate<Message> callback) where Message : class;

        /// <summary>
        /// Unregisters a custom message handler.
        /// </summary>
        /// <typeparam name="Message">The type of the message.</typeparam>
        /// <param name="callback">The callback method to detach.</param>
        void UnregisterMessageHandler<Message>(MessageWithTransporterHandlerDelegate<Message> callback) where Message : class;

        /// <summary>
        /// Registers a custom message handler.
        /// </summary>
        /// <typeparam name="Message">The type of the message.</typeparam>
        /// <param name="callback">The callback method to register.</param>
        IDisposable RegisterMessageHandler<Message>(AsyncMessageWithTransporterHandlerDelegate<Message> callback) where Message : class;

        /// <summary>
        /// Unregisters a custom message handler.
        /// </summary>
        /// <typeparam name="Message">The type of the message.</typeparam>
        /// <param name="callback">The callback method to detach.</param>
        void UnregisterMessageHandler<Message>(AsyncMessageWithTransporterHandlerDelegate<Message> callback) where Message : class;

        /// <summary>
        /// Registers a custom request handler.
        /// </summary>
        /// <typeparam name="Request">The type of the request.</typeparam>
        /// <param name="callback">The callback method to register.</param>
        IDisposable RegisterRequestHandler<Request>(MessageWithTransporterHandlerDelegate<Request> callback) where Request : class;

        /// <summary>
        /// Unregisters a custom request handler.
        /// </summary>
        /// <typeparam name="Request">The type of the request.</typeparam>
        /// <param name="callback">The callback method to detach.</param>
        void UnregisterRequestHandler<Request>(MessageWithTransporterHandlerDelegate<Request> callback) where Request : class;

        /// <summary>
        /// Registers a custom request handler.
        /// </summary>
        /// <typeparam name="Request">The type of the request.</typeparam>
        /// <param name="callback">The callback method to register.</param>
        IDisposable RegisterRequestHandler<Request>(AsyncMessageWithTransporterHandlerDelegate<Request> callback) where Request : class;

        /// <summary>
        /// Unregisters a custom request handler.
        /// </summary>
        /// <typeparam name="Request">The type of the request.</typeparam>
        /// <param name="callback">The callback method to detach.</param>
        void UnregisterRequestHandler<Request>(AsyncMessageWithTransporterHandlerDelegate<Request> callback) where Request : class;

        /// <summary>
        /// Registers a custom request handler.
        /// </summary>
        /// <typeparam name="Request">The type of the request.</typeparam>
        /// <typeparam name="Response">The type of the response.</typeparam>
        /// <param name="callback">The callback method to register.</param>
        IDisposable RegisterRequestHandler<Request, Response>(RequestHandlerDelegate<Request, Response> callback) where Request : class where Response : class;

        /// <summary>
        /// Unregisters a custom request handler.
        /// </summary>
        /// <typeparam name="Request">The type of the request.</typeparam>
        /// <typeparam name="Response">The type of the response.</typeparam>
        /// <param name="callback">The callback method to detach.</param>
        void UnregisterRequestHandler<Request, Response>(RequestHandlerDelegate<Request, Response> callback) where Request : class where Response : class;

        /// <summary>
        /// Registers a custom request handler.
        /// </summary>
        /// <typeparam name="Request">The type of the request.</typeparam>
        /// <typeparam name="Response">The type of the response.</typeparam>
        /// <param name="callback">The callback method to register.</param>
        IDisposable RegisterRequestHandler<Request, Response>(AsyncRequestHandlerDelegate<Request, Response> callback) where Request : class where Response : class;

        /// <summary>
        /// Unregisters a custom request handler.
        /// </summary>
        /// <typeparam name="Request">The type of the request.</typeparam>
        /// <typeparam name="Response">The type of the response.</typeparam>
        /// <param name="callback">The callback method to detach.</param>
        void UnregisterRequestHandler<Request, Response>(AsyncRequestHandlerDelegate<Request, Response> callback) where Request : class where Response : class;

        /// <summary>
        /// Registers a custom request handler.
        /// </summary>
        /// <typeparam name="Request">The type of the request.</typeparam>
        /// <typeparam name="Response">The type of the response.</typeparam>
        /// <param name="callback">The callback method to register.</param>
        IDisposable RegisterRequestHandler<Request, Response>(RequestWithTransporterHandlerDelegate<Request, Response> callback) where Request : class where Response : class;

        /// <summary>
        /// Unregisters a custom request handler.
        /// </summary>
        /// <typeparam name="Request">The type of the request.</typeparam>
        /// <typeparam name="Response">The type of the response.</typeparam>
        /// <param name="callback">The callback method to detach.</param>
        void UnregisterRequestHandler<Request, Response>(RequestWithTransporterHandlerDelegate<Request, Response> callback) where Request : class where Response : class;

        /// <summary>
        /// Registers a custom request handler.
        /// </summary>
        /// <typeparam name="Request">The type of the request.</typeparam>
        /// <typeparam name="Response">The type of the response.</typeparam>
        /// <param name="callback">The callback method to register.</param>
        IDisposable RegisterRequestHandler<Request, Response>(AsyncRequestWithTransporterHandlerDelegate<Request, Response> callback) where Request : class where Response : class;

        /// <summary>
        /// Unregisters a custom request handler.
        /// </summary>
        /// <typeparam name="Request">The type of the request.</typeparam>
        /// <typeparam name="Response">The type of the response.</typeparam>
        /// <param name="callback">The callback method to detach.</param>
        void UnregisterRequestHandler<Request, Response>(AsyncRequestWithTransporterHandlerDelegate<Request, Response> callback) where Request : class where Response : class;

        /// <summary>
        /// Unregisters all request and message handlers.
        /// </summary>
        void ClearHandlers();

        /// <summary>
        /// Unregisters all RPC services.
        /// </summary>
        void ClearServices();

        /// <summary>
        /// Creates a new message/request handler builder.
        /// This makes it easier to register a message or request handler.
        /// </summary>
        IResonanceHandlerBuilder CreateHandlerBuilder();

        /// <summary>
        /// Registers the specified instance as an RPC service.
        /// </summary>
        /// <typeparam name="TInterface">The type of the interface.</typeparam>
        /// <typeparam name="TImplementation">The type of the implementation.</typeparam>
        /// <param name="service">The service instance.</param>
        void RegisterService<TInterface, TImplementation>(TImplementation service) where TInterface : class where TImplementation : TInterface;

        /// <summary>
        /// Registers the service.
        /// </summary>
        /// <typeparam name="TInterface">The type of the interface.</typeparam>
        /// <typeparam name="TImplementation">The type of the implementation.</typeparam>
        /// <param name="creationType">Define how the service instance should be created.</param>
        void RegisterService<TInterface, TImplementation>(RpcServiceCreationType creationType) where TInterface : class where TImplementation : TInterface, new();

        /// <summary>
        /// Registers the specified instance as an RPC service.
        /// </summary>
        /// <typeparam name="TInterface">The type of the interface.</typeparam>\
        /// <typeparam name="TImplementation">The type of the implementation.</typeparam>
        /// <param name="creationType">Define how the service instance should be created.</param>
        /// <param name="createFunc">Provide the service creation function.</param>
        void RegisterService<TInterface, TImplementation>(RpcServiceCreationType creationType, Func<TImplementation> createFunc) where TInterface : class where TImplementation : TInterface;

        /// <summary>
        /// Unregisters the specified RPC service.
        /// </summary>
        /// <typeparam name="TInterface">The type of the interface.</typeparam>
        void UnregisterService<TInterface>() where TInterface : class;

        /// <summary>
        /// Transfers this instance message and request handlers and registered services to the specified instance.
        /// </summary>
        /// <param name="transporter">The transporter to copy the handlers and services to.</param>
        void TransferHandlersAndServices(IResonanceTransporter transporter);

        /// <summary>
        /// Sends the specified request message and returns a response.
        /// </summary>
        /// <typeparam name="Request">The type of the Request.</typeparam>
        /// <typeparam name="Response">The type of the Response.</typeparam>
        /// <param name="request">The request message.</param>
        /// <param name="config">Request configuration.</param>
        /// <returns></returns>
        Task<Response> SendRequestAsync<Request, Response>(Request request, ResonanceRequestConfig config = null);

        /// <summary>
        /// Sends the specified request message and returns a response.
        /// </summary>
        /// <typeparam name="Request">The type of the Request.</typeparam>
        /// <typeparam name="Response">The type of the Response.</typeparam>
        /// <param name="request">The request message.</param>
        /// <param name="config">Request configuration.</param>
        /// <returns></returns>
        Response SendRequest<Request, Response>(Request request, ResonanceRequestConfig config = null);

        /// <summary>
        /// Sends the specified request message and returns a response.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="config">The configuration.</param>
        /// <returns></returns>
        Task<Object> SendRequestAsync(Object request, ResonanceRequestConfig config = null);

        /// <summary>
        /// Sends the specified request message and returns a response.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="config">The configuration.</param>
        /// <returns></returns>
        Object SendRequest(Object request, ResonanceRequestConfig config = null);

        /// <summary>
        /// Sends the specified request message and returns a response.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="config">The configuration.</param>
        /// <returns></returns>
        Task<Object> SendRequestAsync(ResonanceMessage request, ResonanceRequestConfig config = null);

        /// <summary>
        /// Sends the specified request message and returns a response.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="config">The configuration.</param>
        /// <returns></returns>
        Object SendRequest(ResonanceMessage request, ResonanceRequestConfig config = null);

        /// <summary>
        /// Sends the specified object without expecting any response.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="config">The configuration.</param>
        Task SendAsync(ResonanceMessage message, ResonanceMessageConfig config = null);

        /// <summary>
        /// Sends the specified object without expecting any response.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="config">The configuration.</param>
        void Send(ResonanceMessage message, ResonanceMessageConfig config = null);

        /// <summary>
        /// Sends the specified object without expecting any response.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="config">The configuration.</param>
        Task SendAsync(Object message, ResonanceMessageConfig config = null);

        /// <summary>
        /// Sends the specified object without expecting any response.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="config">The configuration.</param>
        void Send(Object message, ResonanceMessageConfig config = null);

        /// <summary>
        /// Sends a request message while expecting multiple response messages with the same token.
        /// </summary>
        /// <typeparam name="Request">The type of the Request.</typeparam>
        /// <typeparam name="Response">The type of the Response.</typeparam>
        /// <param name="request">The request message.</param>
        /// <param name="config">Request configuration.</param>
        /// <returns></returns>
        ResonanceObservable<Response> SendContinuousRequest<Request, Response>(Request request, ResonanceContinuousRequestConfig config = null);

        /// <summary>
        /// Sends a response message.
        /// </summary>
        /// <typeparam name="Response">The type of the Response.</typeparam>
        /// <param name="response">The response message.</param>
        /// <param name="config">Response configuration.</param>
        /// <returns></returns>
        Task SendResponseAsync<Response>(ResonanceMessage<Response> response, ResonanceResponseConfig config = null);

        /// <summary>
        /// Sends a response message.
        /// </summary>
        /// <typeparam name="Response">The type of the Response.</typeparam>
        /// <param name="response">The response message.</param>
        /// <param name="config">Response configuration.</param>
        /// <returns></returns>
        void SendResponse<Response>(ResonanceMessage<Response> response, ResonanceResponseConfig config = null);

        /// <summary>
        /// Sends the specified response message.
        /// </summary>
        /// <param name="message">The response message.</param>
        /// <param name="token">Request token.</param>
        /// <param name="config">Response configuration.</param>
        /// <returns></returns>
        Task SendResponseAsync(Object message, String token, ResonanceResponseConfig config = null);

        /// <summary>
        /// Sends the specified response message.
        /// </summary>
        /// <param name="message">The response message.</param>
        /// <param name="token">Request token.</param>
        /// <param name="config">Response configuration.</param>
        /// <returns></returns>
        void SendResponse(Object message, String token, ResonanceResponseConfig config = null);

        /// <summary>
        /// Sends the specified response message.
        /// </summary>
        /// <param name="response">The response message.</param>
        /// <param name="config">Response configuration.</param>
        /// <returns></returns>
        Task SendResponseAsync(ResonanceMessage response, ResonanceResponseConfig config = null);

        /// <summary>
        /// Sends the specified response message.
        /// </summary>
        /// <param name="response">The response message.</param>
        /// <param name="config">Response configuration.</param>
        /// <returns></returns>
        void SendResponse(ResonanceMessage response, ResonanceResponseConfig config = null);

        /// <summary>
        /// Sends a general error response agnostic to the type of request.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <param name="token">The request token.</param>
        /// <returns></returns>
        Task SendErrorResponseAsync(Exception exception, String token);

        /// <summary>
        /// Sends a general error response agnostic to the type of request.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <param name="token">The request token.</param>
        /// <returns></returns>
        void SendErrorResponse(Exception exception, String token);

        /// <summary>
        /// Sends a general error response agnostic to the type of request.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="token">The request token.</param>
        /// <returns></returns>
        Task SendErrorResponseAsync(String message, string token);

        /// <summary>
        /// Sends a general error response agnostic to the type of request.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="token">The request token.</param>
        /// <returns></returns>
        void SendErrorResponse(String message, string token);

        /// <summary>
        /// Submits encoding information to be written to encoded and written to the adapter.
        /// </summary>
        /// <param name="info">The encoding information.</param>
        void SubmitEncodingInformation(ResonanceEncodingInformation info);

        /// <summary>
        /// Creates a new transporter builder based on this transporter.
        /// </summary>
        IAdapterBuilder CreateBuilder();

        /// <summary>
        /// Disconnects the transporter.
        /// </summary>
        /// <param name="reason">The error message to be presented to the other side.</param>
        void Disconnect(String reason);

        /// <summary>
        /// Disconnects the transporter.
        /// </summary>
        /// <param name="reason">The error message to be presented to the other side.</param>
        Task DisconnectAsync(String reason);

        /// <summary>
        /// Disconnects and disposes this transporter.
        /// </summary>
        /// <param name="withAdapter"><c>true</c> to release the underlying <see cref="Adapter"/> along with this transporter.</param>
        void Dispose(bool withAdapter = false);

        /// <summary>
        /// Disconnects and disposes this transporter.
        /// </summary>
        /// <param name="withAdapter"><c>true</c> to release the underlying <see cref="Adapter"/> along with this transporter.</param>
        Task DisposeAsync(bool withAdapter = false);

        /// <summary>
        /// Returns true if a pending message/request exists by the specified message token.
        /// </summary>
        /// <param name="token">The message/request token.</param>
        bool CheckPending(String token);

        /// <summary>
        /// Creates a client proxy for the specified service interface.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        T CreateClientProxy<T>() where T : class;
    }
}