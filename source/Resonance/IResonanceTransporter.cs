using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Resonance.Reactive;
using static Resonance.ResonanceTransporterBuilder;
using Resonance.HandShake;

namespace Resonance
{
    public delegate void RequestHandlerCallbackDelegate<Request>(IResonanceTransporter transporter, ResonanceRequest<Request> request) where Request : class;

    public delegate ResonanceActionResult<Response> RequestHandlerCallbackDelegate<Request, Response>(Request request) where Request : class where Response : class;

    /// <summary>
    /// Represents a Resonance Transporter capable of sending and receiving request/response messages.
    /// </summary>
    /// <seealso cref="Resonance.IResonanceComponent" />
    /// <seealso cref="Resonance.IResonanceStateComponent" />
    /// <seealso cref="Resonance.IResonanceConnectionComponent" />
    public interface IResonanceTransporter : IResonanceComponent, IResonanceStateComponent, IResonanceConnectionComponent, IDisposable, IResonanceAsyncDisposable
    {
        /// <summary>
        /// Occurs when a new request message has been received.
        /// </summary>
        event EventHandler<ResonanceRequestReceivedEventArgs> RequestReceived;

        /// <summary>
        /// Occurs when a request has been sent.
        /// </summary>
        event EventHandler<ResonanceRequestEventArgs> RequestSent;

        /// <summary>
        /// Occurs when a request has failed.
        /// </summary>
        event EventHandler<ResonanceRequestFailedEventArgs> RequestFailed;

        /// <summary>
        /// Occurs when a request response has been received.
        /// </summary>
        event EventHandler<ResonanceResponseEventArgs> ResponseReceived;

        /// <summary>
        /// Occurs when a response has been sent.
        /// </summary>
        event EventHandler<ResonanceResponseEventArgs> ResponseSent;

        /// <summary>
        /// Occurs when a response has failed to be sent.
        /// </summary>
        event EventHandler<ResonanceResponseFailedEventArgs> ResponseFailed;

        /// <summary>
        /// Occurs when the keep alive mechanism is enabled and has failed by reaching the given timeout and retries.
        /// </summary>
        event EventHandler KeepAliveFailed;

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
        /// Returns true if communication is currently encrypted.
        /// </summary>
        bool IsChannelSecure { get; }

        /// <summary>
        /// Disable the startup handshake.
        /// This will prevent any encryption from happening, and will fail to communicate with Handshake enabled transporters.
        /// </summary>
        bool DisableHandShake { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the transporter will get in to a failed state if the <see cref="Adapter"/> fails.
        /// </summary>
        bool FailsWithAdapter { get; set; }

        /// <summary>
        /// Gets the total number of queued outgoing messages.
        /// </summary>
        int OutgoingQueueCount { get; }

        /// <summary>
        /// Gets the number of current pending requests.
        /// </summary>
        int PendingRequestsCount { get; }

        /// <summary>
        /// Gets the total of incoming messages.
        /// </summary>
        int TotalIncomingMessages { get; }

        /// <summary>
        /// Gets the total of outgoing messages.
        /// </summary>
        int TotalOutgoingMessages { get; }

        /// <summary>
        /// Registers a custom request handler.
        /// </summary>
        /// <typeparam name="Request">The type of the request.</typeparam>
        /// <param name="callback">The callback method to register.</param>
        void RegisterRequestHandler<Request>(RequestHandlerCallbackDelegate<Request> callback) where Request : class;

        /// <summary>
        /// Registers a custom request handler.
        /// </summary>
        /// <typeparam name="Request">The type of the request.</typeparam>
        /// <typeparam name="Response">The type of the response.</typeparam>
        /// <param name="callback">The callback method to register.</param>
        void RegisterRequestHandler<Request, Response>(RequestHandlerCallbackDelegate<Request, Response> callback) where Request : class where Response : class;

        /// <summary>
        /// Unregisters a custom request handler.
        /// </summary>
        /// <typeparam name="Request">The type of the request.</typeparam>
        /// <param name="callback">The callback method to detach.</param>
        void UnregisterRequestHandler<Request>(RequestHandlerCallbackDelegate<Request> callback) where Request : class;

        /// <summary>
        /// Unregisters a custom request handler.
        /// </summary>
        /// <typeparam name="Request">The type of the request.</typeparam>
        /// <typeparam name="Response">The type of the response.</typeparam>
        /// <param name="callback">The callback method to detach.</param>
        void UnregisterRequestHandler<Request, Response>(RequestHandlerCallbackDelegate<Request, Response> callback) where Request : class where Response : class;

        /// <summary>
        /// Registers an instance of <see cref="IResonanceService"/> as a request handler service.
        /// Each method with return type of <see cref="ResonanceActionResult{T}"/> will be registered has a request handler.
        /// Request handler methods should accept only the request as a single parameter.
        /// </summary>
        /// <param name="service">The service.</param>
        void RegisterService(IResonanceService service);

        /// <summary>
        /// Detach the specified <see cref="IResonanceService"/> and all its request handlers.
        /// </summary>
        /// <param name="service">The service.</param>
        void UnregisterService(IResonanceService service);

        /// <summary>
        /// Copies this instance request handlers and registered services to the specified instance.
        /// </summary>
        /// <param name="transporter">The transporter to copy the handlers to.</param>
        void CopyRequestHandlersAndServices(IResonanceTransporter transporter);

        /// <summary>
        /// Sends the specified request message and returns a response.
        /// </summary>
        /// <typeparam name="Request">The type of the Request.</typeparam>
        /// <typeparam name="Response">The type of the Response.</typeparam>
        /// <param name="request">The request message.</param>
        /// <param name="config">Request configuration.</param>
        /// <returns></returns>
        Task<Response> SendRequest<Request, Response>(Request request, ResonanceRequestConfig config = null);

        /// <summary>
        /// Sends the specified request message and returns a response.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="config">The configuration.</param>
        /// <returns></returns>
        Task<Object> SendRequest(Object request, ResonanceRequestConfig config = null);

        /// <summary>
        /// Sends the specified request message and returns a response.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="config">The configuration.</param>
        /// <returns></returns>
        Task<Object> SendRequest(ResonanceRequest request, ResonanceRequestConfig config = null);

        /// <summary>
        /// Sends the specified object without expecting any response.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="config">The configuration.</param>
        Task SendObject(Object message, ResonanceRequestConfig config = null);

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
        Task SendResponse<Response>(ResonanceResponse<Response> response, ResonanceResponseConfig config = null);

        /// <summary>
        /// Sends the specified response message.
        /// </summary>
        /// <param name="message">The response message.</param>
        /// <param name="token">Request token.</param>
        /// <param name="config">Response configuration.</param>
        /// <returns></returns>
        Task SendResponse(Object message, String token, ResonanceResponseConfig config = null);

        /// <summary>
        /// Sends the specified response message.
        /// </summary>
        /// <param name="response">The response message.</param>
        /// <param name="config">Response configuration.</param>
        /// <returns></returns>
        Task SendResponse(ResonanceResponse response, ResonanceResponseConfig config = null);

        /// <summary>
        /// Sends a general error response agnostic to the type of request.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <param name="token">The request token.</param>
        /// <returns></returns>
        Task SendErrorResponse(Exception exception, String token);

        /// <summary>
        /// Sends a general error response agnostic to the type of request.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="token">The request token.</param>
        /// <returns></returns>
        Task SendErrorResponse(String message, string token);

        /// <summary>
        /// Creates a new transporter builder based on this transporter.
        /// </summary>
        IAdapterBuilder CreateBuilder();

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
    }
}