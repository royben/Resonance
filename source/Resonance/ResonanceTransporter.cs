using Resonance.Exceptions;
using Resonance.ExtensionMethods;
using Resonance.Logging;
using Resonance.Reactive;
using Resonance.Threading;
using Resonance.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Resonance.ResonanceTransporterBuilder;

namespace Resonance
{
    /// <summary>
    /// Represents an <see cref="IResonanceTransporter"/> base class.
    /// </summary>
    /// <seealso cref="Resonance.IResonanceTransporter" />
    public class ResonanceTransporter : ResonanceObject, IResonanceTransporter
    {
        private static int _globalTransporterCounter = 1;
        private int _transporterCounter;
        private DateTime _lastIncomingMessageTime;

        private object _disposeLock = new object();

        private PriorityProducerConsumerQueue<Object> _sendingQueue;
        private ConcurrentList<IResonancePendingRequest> _pendingRequests;
        private ProducerConsumerQueue<byte[]> _arrivedMessages;
        private Thread _pushThread;
        private Thread _pullThread;
        private Thread _keepAliveThread;

        #region Events

        /// <summary>
        /// Occurs when a new request message has been received.
        /// </summary>
        public event EventHandler<ResonanceRequestReceivedEventArgs> RequestReceived;

        /// <summary>
        /// Occurs when a request has been sent.
        /// </summary>
        public event EventHandler<ResonanceRequestEventArgs> RequestSent;

        /// <summary>
        /// Occurs when a request has failed.
        /// </summary>
        public event EventHandler<ResonanceRequestFailedEventArgs> RequestFailed;

        /// <summary>
        /// Occurs when a response has been sent.
        /// </summary>
        public event EventHandler<ResonanceResponseEventArgs> ResponseSent;

        /// <summary>
        /// Occurs when a request response has been received.
        /// </summary>
        public event EventHandler<ResonanceResponseEventArgs> ResponseReceived;

        /// <summary>
        /// Occurs when a response has failed to be sent.
        /// </summary>
        public event EventHandler<ResonanceResponseFailedEventArgs> ResponseFailed;

        /// <summary>
        /// Occurs when the current state of the component has changed.
        /// </summary>
        public event EventHandler<ResonanceComponentStateChangedEventArgs> StateChanged;

        /// <summary>
        /// Occurs when the keep alive mechanism is enabled and has failed by reaching the given timeout and retries.
        /// </summary>
        public event EventHandler KeepAliveFailed;

        #endregion

        #region Properties

        private IResonanceAdapter _adapter;
        /// <summary>
        /// Gets or sets the Resonance adapter used to send and receive actual encoded data.
        /// </summary>
        public IResonanceAdapter Adapter
        {
            get { return _adapter; }
            set
            {
                var previous = _adapter;
                _adapter = value;
                OnAdapterChanged(previous, value);
            }
        }

        /// <summary>
        /// Gets or sets the encoder to use for encoding outgoing messages.
        /// </summary>
        public IResonanceEncoder Encoder { get; set; }

        /// <summary>
        /// Gets or sets the decoder to use for decoding incoming messages.
        /// </summary>
        public IResonanceDecoder Decoder { get; set; }

        private ResonanceComponentState _state;
        /// <summary>
        /// Gets the current state of the component.
        /// </summary>
        public ResonanceComponentState State
        {
            get { return _state; }
            set
            {
                if (_state != value)
                {
                    var prev = _state;
                    _state = value;
                    OnStateChanged(prev, _state);
                }
            }
        }

        /// <summary>
        /// Gets or sets the message token generator.
        /// </summary>
        public IResonanceTokenGenerator TokenGenerator { get; set; }

        /// <summary>
        /// Gets the last failed state exception of this component.
        /// </summary>
        public Exception FailedStateException { get; private set; }

        /// <summary>
        /// Gets or sets the default request timeout.
        /// </summary>
        public TimeSpan DefaultRequestTimeout { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the transporter will get in to a failed state if the <see cref="Adapter" /> fails.
        /// </summary>
        public bool FailsWithAdapter { get; set; }

        /// <summary>
        /// Gets or sets the keep alive configuration.
        /// </summary>
        public ResonanceKeepAliveConfiguration KeepAliveConfiguration { get; private set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ResonanceTransporter"/> class.
        /// </summary>
        public ResonanceTransporter()
        {
            _transporterCounter = _globalTransporterCounter++;

            _lastIncomingMessageTime = DateTime.Now;

            KeepAliveConfiguration = new ResonanceKeepAliveConfiguration();
            TokenGenerator = new ShortGuidGenerator();

            _sendingQueue = new PriorityProducerConsumerQueue<object>();
            _pendingRequests = new ConcurrentList<IResonancePendingRequest>();
            _arrivedMessages = new ProducerConsumerQueue<byte[]>();

            DefaultRequestTimeout = TimeSpan.FromSeconds(5);
        }

        #endregion

        #region Request Handlers

        public void RegisterRequestHandler<Request>(RequestHandlerCallbackDelegate<Request> callback) where Request : class
        {
            throw new NotImplementedException();
        }

        public void UnregisterRequestHandler<Request>(RequestHandlerCallbackDelegate<Request> callback) where Request : class
        {
            throw new NotImplementedException();
        }

        public void CopyRequestHandlers(IResonanceTransporter transporter)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Connect/Disconnect

        /// <summary>
        /// Connects this transporter along with the underlying <see cref="Adapter"/>.
        /// </summary>
        /// <returns></returns>
        public async Task Connect()
        {
            if (State == ResonanceComponentState.Connected) return;

            if (Adapter != null)
            {
                await Adapter.Connect();
            }

            State = ResonanceComponentState.Connected;
            StartThreads();

            LogManager.Log($"{this}: Transporter Connected...");
        }

        /// <summary>
        /// Disconnects this transporter along the underlying <see cref="Adapter"/>.
        /// </summary>
        /// <returns></returns>
        public async Task Disconnect()
        {
            if (State == ResonanceComponentState.Connected)
            {
                State = ResonanceComponentState.Disconnected;

                await FinalizeDisconnection();

                LogManager.Log($"{this}: Transporter Disconnected...");
            }
        }

        #endregion

        #region Send Request

        /// <summary>
        /// Sends the specified request message and returns a response.
        /// </summary>
        /// <typeparam name="Request">The type of the Request.</typeparam>
        /// <typeparam name="Response">The type of the Response.</typeparam>
        /// <param name="request">The request message.</param>
        /// <param name="config">Request configuration.</param>
        /// <returns></returns>
        public async Task<Response> SendRequest<Request, Response>(Request request, ResonanceRequestConfig config = null)
        {
            return (Response)await SendRequest(request, config);
        }

        /// <summary>
        /// Sends the specified request message and returns a response.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="config">The configuration.</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException"></exception>
        public Task<Object> SendRequest(Object request, ResonanceRequestConfig config = null)
        {
            ResonanceRequest resonanceRequest = new ResonanceRequest();
            resonanceRequest.Token = TokenGenerator.GenerateToken(request);
            resonanceRequest.Message = request;

            return SendRequest(resonanceRequest, config);
        }

        /// <summary>
        /// Sends the specified request message and returns a response.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="config">The configuration.</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException"></exception>
        public Task<Object> SendRequest(ResonanceRequest request, ResonanceRequestConfig config = null)
        {
            if (State != ResonanceComponentState.Connected)
            {
                throw LogManager.Log(new InvalidOperationException($"{this}: Could not send the request while transporter state is {State}."));
            }

            config = config ?? new ResonanceRequestConfig();
            config.Timeout = config.Timeout ?? DefaultRequestTimeout;

            TaskCompletionSource<Object> completionSource = new TaskCompletionSource<object>();

            ResonancePendingRequest pendingRequest = new ResonancePendingRequest();
            pendingRequest.Request = request;
            pendingRequest.Config = config;
            pendingRequest.CompletionSource = completionSource;

            LogManager.Log($"{this}: Queuing request message: {request.Message.GetType().Name} Token: {request.Token}", LogLevel.Debug);

            _sendingQueue.BlockEnqueue(pendingRequest, config.Priority);

            return completionSource.Task;
        }

        /// <summary>
        /// Sends a request message while expecting multiple response messages with the same token.
        /// </summary>
        /// <typeparam name="Request">The type of the Request.</typeparam>
        /// <typeparam name="Response">The type of the Response.</typeparam>
        /// <param name="request">The request message.</param>
        /// <param name="config">Request configuration.</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException"></exception>
        public ResonanceObservable<Response> SendContinuousRequest<Request, Response>(Request request, ResonanceContinuousRequestConfig config = null)
        {
            if (State != ResonanceComponentState.Connected)
            {
                throw LogManager.Log(new InvalidOperationException($"{this}: Could not send the request while transporter state is {State}."));
            }

            config = config ?? new ResonanceContinuousRequestConfig();
            config.Timeout = config.Timeout ?? DefaultRequestTimeout;
            config.ContinuousTimeout = config.ContinuousTimeout ?? DefaultRequestTimeout;

            ResonanceRequest resonanceRequest = new ResonanceRequest();
            resonanceRequest.Token = TokenGenerator.GenerateToken(request);
            resonanceRequest.Message = request;

            ResonanceObservable<Response> observable = new ResonanceObservable<Response>();

            ResonancePendingContinuousRequest pendingContinuousRequest = new ResonancePendingContinuousRequest();
            pendingContinuousRequest.Request = resonanceRequest;
            pendingContinuousRequest.Config = config;
            pendingContinuousRequest.ContinuousObservable = observable;

            LogManager.Log($"{this}: Queuing continuous request message: {request.GetType().Name} Token: {resonanceRequest.Token}", LogLevel.Debug);

            _sendingQueue.BlockEnqueue(pendingContinuousRequest, config.Priority);

            return observable;
        }

        #endregion

        #region Send Response

        /// <summary>
        /// Sends a response message.
        /// </summary>
        /// <typeparam name="Response">The type of the Response.</typeparam>
        /// <param name="response">The response message.</param>
        /// <param name="config">Response configuration.</param>
        /// <returns></returns>
        public Task SendResponse<Response>(ResonanceResponse<Response> response, ResonanceResponseConfig config = null)
        {
            return SendResponse((ResonanceResponse)response, config);
        }

        /// <summary>
        /// Sends the specified response message.
        /// </summary>
        /// <param name="message">The response message.</param>
        /// <param name="token">Request token.</param>
        /// <param name="config">Response configuration.</param>
        /// <returns></returns>
        public Task SendResponse(Object message, String token, ResonanceResponseConfig config = null)
        {
            return SendResponse(new ResonanceResponse()
            {
                Message = message,
                Token = token
            }, config);
        }

        /// <summary>
        /// Sends the specified response message.
        /// </summary>
        /// <param name="response">The response message.</param>
        /// <param name="config">Response configuration.</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException"></exception>
        public Task SendResponse(ResonanceResponse response, ResonanceResponseConfig config = null)
        {
            if (State != ResonanceComponentState.Connected)
            {
                throw LogManager.Log(new InvalidOperationException($"{this}: Could not send the response while transporter state is {State}."));
            }

            config = config ?? new ResonanceResponseConfig();

            TaskCompletionSource<Object> completionSource = new TaskCompletionSource<object>();

            ResonancePendingResponse pendingResponse = new ResonancePendingResponse();
            pendingResponse.Response = response;
            pendingResponse.CompletionSource = completionSource;
            pendingResponse.Config = config;
            _sendingQueue.BlockEnqueue(pendingResponse, config.Priority);

            return completionSource.Task;
        }

        /// <summary>
        /// Sends a general error response agnostic to the type of request.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <param name="token">The request token.</param>
        /// <returns></returns>
        public Task SendErrorResponse(Exception exception, string token)
        {
            return SendErrorResponse(exception.Message, token);
        }

        /// <summary>
        /// Sends a general error response agnostic to the type of request.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="token">The request token.</param>
        /// <returns></returns>
        public Task SendErrorResponse(String message, string token)
        {
            ResonanceResponseConfig config = new ResonanceResponseConfig();
            config.HasError = true;
            config.ErrorMessage = message;

            return SendResponse(new ResonanceResponse() { Message = message, Token = token }, config);
        }

        #endregion

        #region Push

        private void PushThreadMethod()
        {
            try
            {
                while (State == ResonanceComponentState.Connected)
                {
                    Object pending = _sendingQueue.BlockDequeue();
                    if (pending == null || State != ResonanceComponentState.Connected) return;

                    if (pending is ResonancePendingRequest pendingRequest)
                    {
                        OnOutgoingRequest(pendingRequest);
                    }
                    else if (pending is ResonancePendingContinuousRequest pendingContinuousRequest)
                    {
                        OnOutgoingContinuousRequest(pendingContinuousRequest);
                    }
                    else if (pending is ResonancePendingResponse pendingResponse)
                    {
                        OnOutgoingResponse(pendingResponse);
                    }
                }
            }
            catch (ThreadAbortException)
            {
                LogManager.Log($"{this}: Push thread has been aborted.");
            }
            catch (Exception ex)
            {
                OnFailed(ex);
            }
        }

        /// <summary>
        /// Performs the actual outgoing request sending.
        /// </summary>
        /// <param name="pendingRequest">The pending request.</param>
        protected virtual void OnOutgoingRequest(ResonancePendingRequest pendingRequest)
        {
            try
            {
                if (pendingRequest.Config.ShouldLog)
                {
                    LogManager.Log($"{this}: Sending request '{pendingRequest.Request.Message.GetType()}'...\n{pendingRequest.Request.Message.ToJsonString()}", LogLevel.Info);
                }

                _pendingRequests.Add(pendingRequest);

                ResonanceEncodingInformation info = new ResonanceEncodingInformation();
                info.Token = pendingRequest.Request.Token;
                info.Message = pendingRequest.Request.Message;

                if (pendingRequest.Request.Message is ResonanceKeepAliveRequest)
                {
                    info.Type = ResonanceTranscodingInformationType.KeepAliveRequest;
                }
                else
                {
                    info.Type = ResonanceTranscodingInformationType.Request;
                }

                if (pendingRequest.Config.CancellationToken != null)
                {
                    Task.Factory.StartNew(() =>
                    {
                        while (!pendingRequest.CompletionSource.Task.IsCompleted)
                        {
                            Thread.Sleep(10);

                            if (pendingRequest.Config.CancellationToken.Value.IsCancellationRequested)
                            {
                                _pendingRequests.Remove(pendingRequest);
                                pendingRequest.CompletionSource.SetException(new OperationCanceledException());
                            }
                        }
                    });
                }

                OnEncodeAndWriteData(info);

                Task.Delay(pendingRequest.Config.Timeout.Value).ContinueWith((x) =>
                {
                    if (!pendingRequest.CompletionSource.Task.IsCompleted)
                    {
                        _pendingRequests.Remove(pendingRequest);
                        pendingRequest.CompletionSource.SetException(new TimeoutException($"{pendingRequest.Request.Message.GetType()} was not provided with a response within the given period of {pendingRequest.Config.Timeout.Value.Seconds} seconds and has timed out."));
                    }
                });

                Task.Factory.StartNew(() =>
                {
                    OnRequestSent(pendingRequest.Request);
                });
            }
            catch (Exception ex)
            {
                pendingRequest.CompletionSource.SetException(ex);
            }
        }

        /// <summary>
        /// Performs the actual outgoing continuous request sending.
        /// </summary>
        /// <param name="pendingContinuousRequest">The pending continuous request.</param>
        protected virtual void OnOutgoingContinuousRequest(ResonancePendingContinuousRequest pendingContinuousRequest)
        {
            try
            {
                if (pendingContinuousRequest.Config.ShouldLog)
                {
                    LogManager.Log($"{this}: Sending continuous request '{pendingContinuousRequest.Request.Message.GetType()}'...\n{pendingContinuousRequest.Request.Message.ToJsonString()}", LogLevel.Info);
                }

                _pendingRequests.Add(pendingContinuousRequest);

                ResonanceEncodingInformation info = new ResonanceEncodingInformation();
                info.Token = pendingContinuousRequest.Request.Token;
                info.Message = pendingContinuousRequest.Request.Message;
                info.Type = ResonanceTranscodingInformationType.ContinuousRequest;

                if (pendingContinuousRequest.Config.CancellationToken != null)
                {
                    Task.Factory.StartNew(() =>
                    {
                        while (!pendingContinuousRequest.ContinuousObservable.IsCompleted)
                        {
                            Thread.Sleep(10);

                            if (pendingContinuousRequest.Config.CancellationToken.Value.IsCancellationRequested)
                            {
                                _pendingRequests.Remove(pendingContinuousRequest);
                                pendingContinuousRequest.ContinuousObservable.OnError(new OperationCanceledException());
                            }
                        }
                    });
                }

                OnEncodeAndWriteData(info);

                Task.Delay(pendingContinuousRequest.Config.Timeout.Value).ContinueWith((x) =>
                {
                    if (!pendingContinuousRequest.ContinuousObservable.FirstMessageArrived)
                    {
                        if (pendingContinuousRequest.Config.CancellationToken == null || !pendingContinuousRequest.Config.CancellationToken.Value.IsCancellationRequested)
                        {
                            _pendingRequests.Remove(pendingContinuousRequest);
                            pendingContinuousRequest.ContinuousObservable.OnError(new TimeoutException($"{pendingContinuousRequest.Request.Message.GetType()} was not provided with a response within the given period of {pendingContinuousRequest.Config.Timeout.Value.Seconds} seconds and has timed out."));
                        }
                    }
                    else
                    {
                        if (pendingContinuousRequest.Config.ContinuousTimeout != null)
                        {
                            Task.Factory.StartNew(async () =>
                            {
                                while (!pendingContinuousRequest.ContinuousObservable.IsCompleted)
                                {
                                    await Task.Delay(pendingContinuousRequest.Config.ContinuousTimeout.Value).ContinueWith((y) =>
                                    {
                                        if (!pendingContinuousRequest.ContinuousObservable.IsCompleted)
                                        {
                                            if (DateTime.Now - pendingContinuousRequest.ContinuousObservable.LastResponseTime > pendingContinuousRequest.Config.ContinuousTimeout.Value)
                                            {
                                                TimeoutException ex = new TimeoutException($"{this}: Continuous request message '{pendingContinuousRequest.Request.Message.GetType()}' had failed to provide a response for a period of {pendingContinuousRequest.Config.ContinuousTimeout.Value.TotalSeconds} seconds and has timed out.");
                                                OnRequestFailed(pendingContinuousRequest.Request, ex);
                                                pendingContinuousRequest.ContinuousObservable.OnError(ex);
                                                return;
                                            }
                                        }
                                    });
                                }
                            });
                        }
                    }
                });

                Task.Factory.StartNew(() =>
                {
                    OnRequestSent(pendingContinuousRequest.Request);
                });
            }
            catch (Exception ex)
            {
                pendingContinuousRequest.ContinuousObservable.OnError(ex);
            }
        }

        /// <summary>
        /// Performs the actual outgoing response sending.
        /// </summary>
        /// <param name="pendingResponse">The pending response.</param>
        protected virtual void OnOutgoingResponse(ResonancePendingResponse pendingResponse)
        {
            try
            {
                if (pendingResponse.Config.ShouldLog)
                {
                    LogManager.Log($"{this}: Sending request '{pendingResponse.Response.Message.GetType()}'...\n{pendingResponse.Response.Message.ToJsonString()}", LogLevel.Info);
                }

                ResonanceEncodingInformation info = new ResonanceEncodingInformation();
                info.Token = pendingResponse.Response.Token;
                info.Message = pendingResponse.Response.Message;
                info.Completed = pendingResponse.Config.Completed;
                info.ErrorMessage = pendingResponse.Config.ErrorMessage;
                info.HasError = pendingResponse.Config.HasError;

                if (pendingResponse.Response.Message is ResonanceKeepAliveResponse)
                {
                    info.Type = ResonanceTranscodingInformationType.KeepAliveResponse;
                }
                else
                {
                    info.Type = ResonanceTranscodingInformationType.Response;
                }

                OnEncodeAndWriteData(info);

                pendingResponse.CompletionSource.SetResult(true);

                Task.Factory.StartNew(() =>
                {
                    OnResponseSent(pendingResponse.Response);
                });
            }
            catch (Exception ex)
            {
                pendingResponse.CompletionSource.SetException(ex);
                OnResponseFailed(pendingResponse.Response, ex);
            }
        }

        /// <summary>
        /// Performs the actual encoding and adapter writing.
        /// </summary>
        /// <param name="info">The information.</param>
        protected virtual void OnEncodeAndWriteData(ResonanceEncodingInformation info)
        {
            byte[] data = Encoder.Encode(info);
            Adapter.Write(data);
        }

        #endregion

        #region Pull

        private void PullThreadMethod()
        {
            try
            {
                while (State == ResonanceComponentState.Connected)
                {
                    byte[] data = _arrivedMessages.BlockDequeue();
                    if (data == null || State != ResonanceComponentState.Connected) return;

                    try
                    {
                        ResonanceDecodingInformation info = new ResonanceDecodingInformation();

                        try
                        {
                            Decoder.Decode(data, info);
                        }
                        catch (Exception ex)
                        {
                            LogManager.Log(ex, $"{this}: Error decoding incoming message.");
                            info.DecoderException = ex;
                        }

                        if (info.Type == ResonanceTranscodingInformationType.Request || info.Type == ResonanceTranscodingInformationType.ContinuousRequest)
                        {
                            if (!info.HasDecodingException)
                            {
                                OnIncomingRequest(info);
                            }
                        }
                        else if (info.Type == ResonanceTranscodingInformationType.KeepAliveRequest)
                        {
                            OnKeepAliveRequestReceived(info);
                        }
                        else
                        {
                            IResonancePendingRequest pending = _pendingRequests.ToList().FirstOrDefault(x => x.Request.Token == info.Token);

                            if (pending != null)
                            {
                                if (pending is ResonancePendingRequest pendingRequest)
                                {
                                    OnIncomingResponse(pendingRequest, info);
                                }
                                else if (pending is ResonancePendingContinuousRequest pendingContinuousRequest)
                                {
                                    OnIncomingContinuousResponse(pendingContinuousRequest, info);
                                }
                            }
                            else
                            {
                                LogManager.Log($"{this}: A response message with no awaiting request was identified. Token: {info.Token}. Message ignored.", LogLevel.Warning);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogManager.Log(ex, "Unexpected error has occurred while handling an incoming message.");
                    }
                }
            }
            catch (ThreadAbortException)
            {
                LogManager.Log($"{this}: Pull thread has been aborted.");
            }
            catch (Exception ex)
            {
                OnFailed(ex);
            }
        }

        /// <summary>
        /// Handles incoming request messages.
        /// </summary>
        /// <param name="info">The information.</param>
        protected virtual void OnIncomingRequest(ResonanceDecodingInformation info)
        {
            ResonanceRequest request = new ResonanceRequest();
            request.Token = info.Token;
            request.Message = info.Message;

            Task.Factory.StartNew(() =>
            {
                OnRequestReceived(request);
            });
        }

        /// <summary>
        /// Handles incoming response messages.
        /// </summary>
        /// <param name="pendingRequest">The pending request.</param>
        /// <param name="info">The information.</param>
        protected virtual void OnIncomingResponse(ResonancePendingRequest pendingRequest, ResonanceDecodingInformation info)
        {
            _pendingRequests.Remove(pendingRequest);

            if (info.HasDecodingException && info.Token != null)
            {
                pendingRequest.CompletionSource.SetException(info.DecoderException);
            }
            else if (!info.HasError)
            {
                pendingRequest.CompletionSource.SetResult(info.Message);

                Task.Factory.StartNew(() =>
                {
                    ResonanceResponse response = new ResonanceResponse();
                    response.Token = info.Token;
                    response.Message = info.Message;
                    OnResponseReceived(response);
                });
            }
            else
            {
                pendingRequest.CompletionSource.SetException(new ResonanceResponseException(info.ErrorMessage));
            }
        }

        /// <summary>
        /// Handles incoming continuous response messages.
        /// </summary>
        /// <param name="pendingContinuousRequest">The pending continuous request.</param>
        /// <param name="info">The information.</param>
        protected virtual void OnIncomingContinuousResponse(ResonancePendingContinuousRequest pendingContinuousRequest, ResonanceDecodingInformation info)
        {
            if (info.HasDecodingException && info.Token != null)
            {
                Task.Factory.StartNew(() =>
                {
                    pendingContinuousRequest.ContinuousObservable.OnError(info.DecoderException);
                });
            }
            else if (!info.HasError)
            {
                ResonanceResponse response = new ResonanceResponse();
                response.Token = info.Token;
                response.Message = info.Message;

                if (!info.Completed)
                {
                    Task.Factory.StartNew(() =>
                    {
                        pendingContinuousRequest.ContinuousObservable.OnNext(info.Message);
                        OnResponseReceived(response);
                    });
                }
                else
                {
                    _pendingRequests.Remove(pendingContinuousRequest);

                    Task.Factory.StartNew(() =>
                    {
                        pendingContinuousRequest.ContinuousObservable.OnNext(info.Message);
                        pendingContinuousRequest.ContinuousObservable.OnCompleted();
                        OnResponseReceived(response);
                    });
                }
            }
            else
            {
                Task.Factory.StartNew(() =>
                {
                    pendingContinuousRequest.ContinuousObservable.OnError(new ResonanceResponseException(info.ErrorMessage));
                });
            }
        }

        /// <summary>
        /// Called when an incoming keep alive request has been received.
        /// </summary>
        /// <param name="info">The decoding information.</param>
        protected async virtual void OnKeepAliveRequestReceived(ResonanceDecodingInformation info)
        {
            if (KeepAliveConfiguration.EnableAutoResponse)
            {
                await SendResponse(new ResonanceKeepAliveResponse(), info.Token);
            }
        }

        #endregion

        #region KeepAlive

        private void KeepAliveThreadMethod()
        {
            int retryCounter = 0;

            while (State == ResonanceComponentState.Connected)
            {
                if (KeepAliveConfiguration.Enabled)
                {
                    try
                    {
                        retryCounter++;

                        var response = SendRequest<ResonanceKeepAliveRequest, ResonanceKeepAliveResponse>(new ResonanceKeepAliveRequest(), new ResonanceRequestConfig()
                        {
                            Priority = QueuePriority.High
                        }).GetAwaiter().GetResult();

                        retryCounter = 0;
                    }
                    catch (TimeoutException)
                    {
                        if (KeepAliveConfiguration.Enabled)
                        {
                            if (DateTime.Now - _lastIncomingMessageTime > DefaultRequestTimeout)
                            {
                                if (retryCounter >= KeepAliveConfiguration.Retries)
                                {
                                    var keepAliveException = new ResonanceKeepAliveException("The transporter has not received a KeepAlive response within the given time.");
                                    LogManager.Log(keepAliveException);
                                    OnKeepAliveFailed();

                                    if (KeepAliveConfiguration.FailTransporterOnTimeout)
                                    {
                                        OnFailed(keepAliveException);
                                        return;
                                    }
                                    else
                                    {
                                        retryCounter = 0;
                                    }
                                }
                                else
                                {
                                    LogManager.Log($"{this}: The transporter has not received a KeepAlive response within the given time. Retrying ({retryCounter}/{KeepAliveConfiguration.Retries})...", LogLevel.Warning);
                                }
                            }
                            else
                            {
                                retryCounter = 0;
                                LogManager.Log($"{this}: The transporter has not received a KeepAlive response within the given time, but was rescued due to other message received within the given time.", LogLevel.Warning);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogManager.Log(ex, $"{this}: Error occurred on keep alive mechanism.");
                    }
                }

                Thread.Sleep((int)Math.Max(KeepAliveConfiguration.Interval.TotalMilliseconds, 500));
            }
        }

        #endregion

        #region Start/Stop Threads

        private void StartThreads()
        {
            _sendingQueue = new PriorityProducerConsumerQueue<object>();
            _pendingRequests = new ConcurrentList<IResonancePendingRequest>();
            _arrivedMessages = new ProducerConsumerQueue<byte[]>();

            _pullThread = new Thread(PullThreadMethod);
            _pullThread.Name = $"{this} Pull Thread";
            _pullThread.IsBackground = true;
            _pullThread.Start();

            _pushThread = new Thread(PushThreadMethod);
            _pushThread.Name = $"{this} Push Thread";
            _pushThread.IsBackground = true;
            _pushThread.Start();

            _keepAliveThread = new Thread(KeepAliveThreadMethod);
            _keepAliveThread.Name = $"{this} KeepAlive Thread";
            _keepAliveThread.IsBackground = true;
            _keepAliveThread.Start();
        }

        private Task StopThreads()
        {
            return Task.Factory.StartNew(() =>
            {
                _sendingQueue.BlockEnqueue(null);
                _arrivedMessages.BlockEnqueue(null);
                _pushThread.Join();
                _pullThread.Join();
            });
        }

        #endregion

        #region Disconnection Procedures

        /// <summary>
        /// Called when the transporter has failed.
        /// </summary>
        /// <param name="exception">The failed exception.</param>
        protected virtual async void OnFailed(Exception exception)
        {
            if (State != ResonanceComponentState.Failed)
            {
                FailedStateException = exception;
                LogManager.Log(exception, $"{this}: Transporter failed.");
                State = ResonanceComponentState.Failed;

                await FinalizeDisconnection();
            }
            else
            {
                LogManager.Log(exception, LogLevel.Warning, $"{this}: OnFailed called while state is already failed!");
            }
        }

        /// <summary>
        /// Performs disconnection final procedures.
        /// </summary>
        /// <returns></returns>
        protected virtual async Task FinalizeDisconnection()
        {
            await StopThreads();

            if (Adapter != null)
            {
                await Adapter.Disconnect();
            }

            NotifyActiveMessagesAboutDisconnection();
        }

        /// <summary>
        /// Notifies all active messages about the transporter disconnection.
        /// </summary>
        protected virtual void NotifyActiveMessagesAboutDisconnection()
        {
            LogManager.Log("Notifying all continuous request messages about disconnection...");
            foreach (var pending in _pendingRequests.ToList())
            {
                try
                {
                    _pendingRequests.Remove(pending);
                    LogManager.Log($"Notifying continuous request '{pending.Request.GetType().Name}'...");
                    var exception = new ResonanceTransporterDisconnectedException("Transporter disconnected.");
                    OnRequestFailed(pending.Request, exception);

                    if (pending is ResonancePendingContinuousRequest continuousPendingRequest)
                    {
                        continuousPendingRequest.ContinuousObservable.OnError(exception);
                    }
                    else if (pending is ResonancePendingRequest pendingRequest)
                    {
                        if (!pendingRequest.CompletionSource.Task.IsCompleted)
                        {
                            pendingRequest.CompletionSource.SetException(exception);
                        }
                    }
                    else if (pending is ResonancePendingResponse pendingResponse)
                    {
                        if (!pendingResponse.CompletionSource.Task.IsCompleted)
                        {
                            pendingResponse.CompletionSource.SetException(exception);
                        }
                    }
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e.ToString());
                }
            }
        }

        #endregion

        #region Events Notification Methods

        /// <summary>
        /// Called when a request has been received.
        /// </summary>
        /// <param name="request">The request.</param>
        protected virtual void OnRequestReceived(ResonanceRequest request)
        {
            RequestReceived?.Invoke(this, new ResonanceRequestReceivedEventArgs(request));
        }

        /// <summary>
        /// Called when a request has been sent.
        /// </summary>
        /// <param name="request">The request.</param>
        protected virtual void OnRequestSent(ResonanceRequest request)
        {
            RequestSent?.Invoke(this, new ResonanceRequestEventArgs(request));
        }

        /// <summary>
        /// Called when a request has failed.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="exception">The exception.</param>
        protected virtual void OnRequestFailed(ResonanceRequest request, Exception exception)
        {
            RequestFailed?.Invoke(this, new ResonanceRequestFailedEventArgs(request, exception));
        }

        /// <summary>
        /// Called when a response has been sent.
        /// </summary>
        /// <param name="response">The response.</param>
        protected virtual void OnResponseSent(ResonanceResponse response)
        {
            ResponseSent?.Invoke(this, new ResonanceResponseEventArgs(response));
        }

        /// <summary>
        /// Called when a response has been received.
        /// </summary>
        /// <param name="response">The response.</param>
        protected virtual void OnResponseReceived(ResonanceResponse response)
        {
            ResponseReceived?.Invoke(this, new ResonanceResponseEventArgs(response));
        }

        /// <summary>
        /// Called when a response has failed.
        /// </summary>
        /// <param name="response">The response.</param>
        /// <param name="exception">The exception.</param>
        protected virtual void OnResponseFailed(ResonanceResponse response, Exception exception)
        {
            ResponseFailed?.Invoke(this, new ResonanceResponseFailedEventArgs(response, exception));
        }

        /// <summary>
        /// Called when when the keep alive mechanism is enabled and has failed.
        /// </summary>
        protected virtual void OnKeepAliveFailed()
        {
            KeepAliveFailed?.Invoke(this, new EventArgs());
        }

        #endregion

        #region Property Changes

        /// <summary>
        /// Called when the transporter <see cref="State"/> has changed.
        /// </summary>
        /// <param name="previousState">Previous state.</param>
        /// <param name="newState">New state.</param>
        protected virtual void OnStateChanged(ResonanceComponentState previousState, ResonanceComponentState newState)
        {
            StateChanged?.Invoke(this, new ResonanceComponentStateChangedEventArgs(previousState, newState));
        }

        /// <summary>
        /// Called when the <see cref="Adapter"/> has changed.
        /// </summary>
        /// <param name="previousAdapter">The previous adapter if any.</param>
        /// <param name="newAdapter">The new adapter.</param>
        protected virtual void OnAdapterChanged(IResonanceAdapter previousAdapter, IResonanceAdapter newAdapter)
        {
            if (previousAdapter != newAdapter)
            {
                _pendingRequests.Clear();
                _arrivedMessages = new ProducerConsumerQueue<byte[]>();
                _sendingQueue = new PriorityProducerConsumerQueue<object>();
            }

            if (previousAdapter != null)
            {
                previousAdapter.StateChanged -= OnAdapterStateChanged;
                previousAdapter.DataAvailable -= OnAdapterDataAvailable;
            }

            LogManager.Log($"{this}: Adapter Changed: {newAdapter}");

            if (newAdapter != null)
            {
                newAdapter.StateChanged -= OnAdapterStateChanged;
                newAdapter.DataAvailable -= OnAdapterDataAvailable;
                newAdapter.StateChanged += OnAdapterStateChanged;
                newAdapter.DataAvailable += OnAdapterDataAvailable;
            }
        }

        #endregion

        #region Adapter Events

        /// <summary>
        /// Called when the adapter reports on new encoded data available.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="ResonanceAdapterDataAvailableEventArgs"/> instance containing the event data.</param>
        protected virtual void OnAdapterDataAvailable(object sender, ResonanceAdapterDataAvailableEventArgs e)
        {
            _lastIncomingMessageTime = DateTime.Now;
            _arrivedMessages.BlockEnqueue(e.Data);
        }

        /// <summary>
        /// Called when the <see cref="Adapter"/> state has changed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="ResonanceComponentStateChangedEventArgs"/> instance containing the event data.</param>
        protected virtual void OnAdapterStateChanged(object sender, ResonanceComponentStateChangedEventArgs e)
        {
            if (e.NewState == ResonanceComponentState.Failed && FailsWithAdapter)
            {
                OnFailed(new ResonanceAdapterFailedException($"The adapter has failed with exception '{Adapter.FailedStateException.Message}' and the transporter is configured to fail with the adapter.", Adapter.FailedStateException));
            }
        }

        #endregion

        #region ToString

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            String encoder = Encoder != null ? Encoder.ToString() : "No Encoder";
            String decoder = Decoder != null ? Decoder.ToString() : "No Decoder";
            String adapter = Adapter != null ? Adapter.ToString() : "No Adapter";

            return $"Transporter {_transporterCounter} => {encoder} / {decoder} => {adapter}";
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Disconnects and disposes this transporter.
        /// </summary>
        public void Dispose()
        {
            lock (_disposeLock)
            {
                if (State != ResonanceComponentState.Disposed)
                {
                    Disconnect().Wait();
                    State = ResonanceComponentState.Disposed;
                }
            }
        }

        /// <summary>
        /// Disconnects and disposes this transporter.
        /// </summary>
        /// <param name="withAdapter"><c>true</c> to release the underlying <see cref="Adapter"/> along with this transporter.</param>
        public void Dispose(bool withAdapter = false)
        {
            Dispose();

            if (withAdapter)
            {
                Adapter.Dispose();
            }
        }

        #endregion

        #region Builder

        /// <summary>
        /// Gets a new transporter builder.
        /// </summary>
        public static IResonanceTransporterBuilder Builder
        {
            get { return ResonanceTransporterBuilder.New(); }
        }

        /// <summary>
        /// Creates a new transporter builder based on this transporter.
        /// </summary>
        public IAdapterBuilder CreateBuilder()
        {
            return ResonanceTransporterBuilder.From(this);
        }

        #endregion
    }
}
