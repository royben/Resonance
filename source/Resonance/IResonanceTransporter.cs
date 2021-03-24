using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Resonance.Reactive;

namespace Resonance
{
    public delegate void RequestHandlerCallbackDelegate<Request>(IResonanceTransporter transporter, ResonanceRequest<Request> request);

    /// <summary>
    /// Represents a Resonance Transporter capable of sending and receiving request/response messages.
    /// </summary>
    /// <seealso cref="Resonance.IResonanceComponent" />
    /// <seealso cref="Resonance.IResonanceStateComponent" />
    /// <seealso cref="Resonance.IResonanceConnectionComponent" />
    public interface IResonanceTransporter : IResonanceComponent, IResonanceStateComponent, IResonanceConnectionComponent
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
        /// Gets or sets the keep alive configuration.
        /// </summary>
        ResonanceKeepAliveConfiguration KeepAliveConfiguration { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the transporter will get in to a failed state if the <see cref="Adapter"/> fails.
        /// </summary>
        bool FailsWithAdapter { get; set; }

        /// <summary>
        /// Registers a custom request handler.
        /// </summary>
        /// <typeparam name="Request">The type of the request.</typeparam>
        /// <param name="callback">The callback.</param>
        void RegisterRequestHandler<Request>(RequestHandlerCallbackDelegate<Request> callback) where Request : class;

        /// <summary>
        /// Unregisters a custom request handler.
        /// </summary>
        /// <typeparam name="Request">The type of the request.</typeparam>
        /// <param name="callback">The callback.</param>
        void UnregisterRequestHandler<Request>(RequestHandlerCallbackDelegate<Request> callback) where Request : class;

        /// <summary>
        /// Copies this instance request handlers to the specified instance.
        /// </summary>
        /// <param name="transporter">The transporter to copy the handlers to.</param>
        void CopyRequestHandlers(IResonanceTransporter transporter);

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
    }
}
