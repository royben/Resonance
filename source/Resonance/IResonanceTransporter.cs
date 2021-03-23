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
    /// Represents a transportation engine which can send and receive messages using a <see cref="IResonanceAdapter">Transport adapter</see>.
    /// </summary>
    /// <seealso cref="Tango.Transport.ITransportComponent" />
    public interface IResonanceTransporter : IResonanceComponent, IResonanceStateComponent, IResonanceConnectionComponent
    {
        /// <summary>
        /// Occurs when a new request message has been received.
        /// </summary>
        event EventHandler<ResonanceRequestReceivedEventArgs> RequestReceived;

        /// <summary>
        /// Occurs when a new response message has been received.
        /// </summary>
        event EventHandler<ResonanceResponse> PendingResponseReceived;

        /// <summary>
        /// Occurs when a request has been sent.
        /// </summary>
        event EventHandler<ResonanceRequest> RequestSent;

        event EventHandler<ResonanceResponse> ResponseSent;

        /// <summary>
        /// Occurs when a request response has been received.
        /// </summary>
        event EventHandler<ResonanceResponse> ResponseReceived;

        /// <summary>
        /// Occurs when a request has failed.
        /// </summary>
        event EventHandler<ResonanceRequestFailedEventArgs> RequestFailed;

        /// <summary>
        /// Gets or sets the <see cref="IResonanceAdapter"/> used to read and write raw data.
        /// </summary>
        IResonanceAdapter Adapter { get; set; }

        /// <summary>
        /// Gets or sets the transport encoder used to encode messages.
        /// </summary>
        IResonanceEncoder Encoder { get; set; }

        /// <summary>
        /// Gets or sets the transport encoder used to decode messages.
        /// </summary>
        IResonanceDecoder Decoder { get; set; }

        IResonanceTokenGenerator TokenGenerator { get; set; }

        /// <summary>
        /// Gets or sets the default request timeout.
        /// </summary>
        TimeSpan RequestTimeout { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to use a keep alive mechanism.
        /// </summary>
        bool UseKeepAlive { get; set; }

        /// <summary>
        /// Gets or sets the keep alive timeout.
        /// </summary>
        TimeSpan KeepAliveTimeout { get; set; }

        /// <summary>
        /// Gets or sets the keep alive retries.
        /// </summary>
        int KeepAliveRetries { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to auto respond to keep alive requests.
        /// </summary>
        bool EnableKeepAliveAutoResponse { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the transporter will get in to a failed state if the <see cref="Adapter"/> has failed.
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
        /// Sends a request.
        /// </summary>
        /// <typeparam name="Request">The type of the request.</typeparam>
        /// <typeparam name="Response">The type of the response.</typeparam>
        /// <param name="request">The request.</param>
        /// <param name="config">Request configuration.</param>
        /// <returns></returns>
        Task<Response> SendRequest<Request, Response>(Request request, ResonanceRequestConfig config = null);

        /// <summary>
        /// Sends a request and expecting multiple response messages.
        /// </summary>
        /// <typeparam name="Request">The type of the request.</typeparam>
        /// <typeparam name="Response">The type of the response.</typeparam>
        /// <param name="config">Request configuration.</param>
        /// <returns></returns>
        ResonanceObservable<Response> SendContinuousRequest<Request, Response>(Request request, ResonanceContinuousRequestConfig config = null);

        /// <summary>
        /// Sends the response.
        /// </summary>
        /// <param name="response">The container.</param>
        /// <returns></returns>
        Task SendResponse<Response>(ResonanceResponse<Response> response, ResonanceResponseConfig config = null);

        Task SendResponse(Object message, String token, ResonanceResponseConfig config = null);

        Task SendResponse(ResonanceResponse response, ResonanceResponseConfig config = null);

        /// <summary>
        /// Sends a general error response agnostic to the type of request.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <param name="token">Request token.</param>
        /// <returns></returns>
        Task SendErrorResponse(Exception exception, String token);

        Task SendErrorResponse(String message, string token);
    }
}
