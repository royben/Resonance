using Microsoft.Extensions.Logging;
using Resonance.Exceptions;
using Resonance.ExtensionMethods;
using Resonance.HandShake;
using Resonance.Reactive;
using Resonance.Threading;
using Resonance.Tokens;
using Resonance.Transcoding.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
        private readonly int _componentCounter;
        private DateTime _lastIncomingMessageTime;
        private PriorityProducerConsumerQueue<Object> _sendingQueue;
        private ConcurrentList<IResonancePendingRequest> _pendingRequests;
        private ProducerConsumerQueue<byte[]> _arrivedMessages;
        private readonly List<ResonanceRequestHandler> _requestHandlers;
        private readonly List<IResonanceService> _services;
        private Thread _pushThread;
        private Thread _pullThread;
        private Thread _keepAliveThread;
        private bool _clearedQueues;
        private bool _isDisposing;
        private bool _preventHandshake;

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

        /// <summary>
        /// Occurs when the Transporter has lost the connection either because the connection has failed, adapter failed 
        /// or the remote peer has disconnected and Disconnect request was received.
        /// </summary>
        public event EventHandler<ResonanceConnectionLostEventArgs> ConnectionLost;

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
        /// Gets or sets the hand shake negotiator.
        /// </summary>
        public IResonanceHandShakeNegotiator HandShakeNegotiator { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to send a disconnection notification to the other side when disconnecting.
        /// </summary>
        public bool NotifyOnDisconnect { get; set; }

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

        /// <summary>
        /// Gets the cryptography configuration.
        /// </summary>
        public ResonanceCryptographyConfiguration CryptographyConfiguration { get; }

        /// <summary>
        /// Returns true if communication is currently encrypted.
        /// </summary>
        public bool IsChannelSecure { get; private set; }

        /// <summary>
        /// Gets the total number of queued outgoing messages.
        /// </summary>
        public int OutgoingQueueCount
        {
            get
            {
                return _sendingQueue.Count;
            }
        }

        /// <summary>
        /// Gets the number of current pending requests.
        /// </summary>
        public int PendingRequestsCount
        {
            get
            {
                return _pendingRequests.Count;
            }
        }

        /// <summary>
        /// Gets the total of incoming messages.
        /// </summary>
        public int TotalIncomingMessages { get; private set; }

        /// <summary>
        /// Gets the total of outgoing messages.
        /// </summary>
        public int TotalOutgoingMessages { get; private set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ResonanceTransporter"/> class.
        /// </summary>
        public ResonanceTransporter()
        {
            _componentCounter = ResonanceComponentCounterManager.Default.GetIncrement(this);

            ClearQueues();

            HandShakeNegotiator = new ResonanceDefaultHandShakeNegotiator(this);

            _requestHandlers = new List<ResonanceRequestHandler>();
            _services = new List<IResonanceService>();

            _lastIncomingMessageTime = DateTime.Now;

            NotifyOnDisconnect = true;

            KeepAliveConfiguration = ResonanceGlobalSettings.Default.DefaultKeepAliveConfiguration();
            CryptographyConfiguration = new ResonanceCryptographyConfiguration(); //Should be set by GlobalSettings.
            TokenGenerator = new ShortGuidGenerator();
            _sendingQueue = new PriorityProducerConsumerQueue<object>();
            _pendingRequests = new ConcurrentList<IResonancePendingRequest>();
            _arrivedMessages = new ProducerConsumerQueue<byte[]>();

            DefaultRequestTimeout = ResonanceGlobalSettings.Default.DefaultRequestTimeout;

            FailsWithAdapter = true;
        }

        #endregion

        #region Request Handlers

        /// <summary>
        /// Registers a custom request handler.
        /// </summary>
        /// <typeparam name="Request">The type of the request.</typeparam>
        /// <param name="callback">The callback method to register.</param>
        public void RegisterRequestHandler<Request>(RequestHandlerCallbackDelegate<Request> callback) where Request : class
        {
            String handlerDescription = $"{callback.Method.DeclaringType.Name}.{callback.Method.Name}";

            Logger.LogDebug("Registering request handler for '{Message}' on '{Handler}'...", typeof(Request).Name, handlerDescription);

            if (!_requestHandlers.Exists(x => (x.RegisteredCallback as RequestHandlerCallbackDelegate<Request>) == callback))
            {
                ResonanceRequestHandler handler = new ResonanceRequestHandler();
                handler.RequestType = typeof(Request);
                handler.RegisteredCallback = callback;
                handler.RegisteredCallbackDescription = handlerDescription;
                handler.Callback = (transporter, resonanceRequest) =>
                {
                    callback?.Invoke(transporter, resonanceRequest as ResonanceRequest<Request>);
                };

                _requestHandlers.Add(handler);
            }
            else
            {
                Logger.LogWarning("Request handler for '{Message}' on '{Handler}' was already registered.", typeof(Request).Name, handlerDescription);
            }
        }

        /// <summary>
        /// Unregisters a custom request handler.
        /// </summary>
        /// <typeparam name="Request">The type of the request.</typeparam>
        /// <param name="callback">The callback method to detach.</param>
        public void UnregisterRequestHandler<Request>(RequestHandlerCallbackDelegate<Request> callback) where Request : class
        {
            Logger.LogDebug("Unregistering request handler for '{Message}' on '{Handler}'...", typeof(Request).Name, $"{callback.Method.DeclaringType}.{callback.Method.Name}");

            var handler = _requestHandlers.FirstOrDefault(x => (x.RegisteredCallback as RequestHandlerCallbackDelegate<Request>) == callback);
            if (handler != null)
            {
                _requestHandlers.Remove(handler);
            }
        }

        /// <summary>
        /// Registers a custom request handler.
        /// </summary>
        /// <typeparam name="Request">The type of the request.</typeparam>
        /// <typeparam name="Response">The type of the response.</typeparam>
        /// <param name="callback">The callback method to register.</param>
        public void RegisterRequestHandler<Request, Response>(RequestHandlerCallbackDelegate<Request, Response> callback) where Request : class where Response : class
        {
            String handlerDescription = $"{callback.Method.DeclaringType.Name}.{callback.Method.Name}";

            Logger.LogDebug("Registering request handler for '{Message}' on '{Handler}'...", typeof(Request).Name, handlerDescription);

            if (!_requestHandlers.Exists(x => (x.RegisteredCallback as RequestHandlerCallbackDelegate<Request, Response>) == callback))
            {
                ResonanceRequestHandler handler = new ResonanceRequestHandler();
                handler.HasResponse = true;
                handler.RequestType = typeof(Request);
                handler.RegisteredCallback = callback;
                handler.RegisteredCallbackDescription = handlerDescription;
                handler.ResponseCallback = (request) =>
                {
                    return (ResonanceActionResult<Response>)callback.Invoke(request as Request);
                };

                _requestHandlers.Add(handler);
            }
            else
            {
                Logger.LogWarning("Request handler for '{Message}' on '{Handler}' was already registered.", typeof(Request).Name, handlerDescription);
            }
        }

        /// <summary>
        /// Unregisters a custom request handler.
        /// </summary>
        /// <typeparam name="Request">The type of the request.</typeparam>
        /// <typeparam name="Response">The type of the response.</typeparam>
        /// <param name="callback">The callback method to detach.</param>
        public void UnregisterRequestHandler<Request, Response>(RequestHandlerCallbackDelegate<Request, Response> callback) where Request : class where Response : class
        {
            Logger.LogDebug("Unregistering request handler for '{Message}' on '{Handler}'...", typeof(Request).Name, $"{callback.Method.DeclaringType}.{callback.Method.Name}");

            var handler = _requestHandlers.FirstOrDefault(x => (x.RegisteredCallback as RequestHandlerCallbackDelegate<Request, Response>) == callback);
            if (handler != null)
            {
                _requestHandlers.Remove(handler);
            }
        }

        /// <summary>
        /// Unregisters a custom request handler.
        /// </summary>
        /// <typeparam name="Request">The type of the request.</typeparam>
        /// <typeparam name="Response">The type of the response.</typeparam>
        /// <param name="callback">The callback method to detach.</param>
        public void RegisterRequestHandler<Request, Response>(RequestHandlerResponseWithTransporterCallbackDelegate<Request, Response> callback) where Request : class where Response : class
        {
            String handlerDescription = $"{callback.Method.DeclaringType.Name}.{callback.Method.Name}";

            Logger.LogDebug("Registering request handler for '{Message}' on '{Handler}'...", typeof(Request).Name, handlerDescription);

            if (!_requestHandlers.Exists(x => (x.RegisteredCallback as RequestHandlerResponseWithTransporterCallbackDelegate<Request, Response>) == callback))
            {
                ResonanceRequestHandler handler = new ResonanceRequestHandler();
                handler.HasResponse = true;
                handler.RequestType = typeof(Request);
                handler.RegisteredCallback = callback;
                handler.RegisteredCallbackDescription = handlerDescription;
                handler.ResponseCallback = (request) =>
                {
                    return (ResonanceActionResult<Response>)callback.Invoke(this, request as Request);
                };

                _requestHandlers.Add(handler);
            }
            else
            {
                Logger.LogWarning("Request handler for '{Message}' on '{Handler}' was already registered.", typeof(Request).Name, handlerDescription);
            }
        }

        /// <summary>
        /// Unregisters a custom request handler.
        /// </summary>
        /// <typeparam name="Request">The type of the request.</typeparam>
        /// <typeparam name="Response">The type of the response.</typeparam>
        /// <param name="callback">The callback method to detach.</param>
        public void UnregisterRequestHandler<Request, Response>(RequestHandlerResponseWithTransporterCallbackDelegate<Request, Response> callback) where Request : class where Response : class
        {
            Logger.LogDebug("Unregistering request handler for '{Message}' on '{Handler}'...", typeof(Request).Name, $"{callback.Method.DeclaringType}.{callback.Method.Name}");

            var handler = _requestHandlers.FirstOrDefault(x => (x.RegisteredCallback as RequestHandlerResponseWithTransporterCallbackDelegate<Request, Response>) == callback);
            if (handler != null)
            {
                _requestHandlers.Remove(handler);
            }
        }

        /// <summary>
        /// Unregisters a custom request handler.
        /// </summary>
        /// <typeparam name="Request">The type of the request.</typeparam>
        /// <typeparam name="Response">The type of the response.</typeparam>
        /// <param name="callback">The callback method to detach.</param>
        public void RegisterRequestHandler<Request, Response>(RequestHandlerCallbackTaskResponseWithTransporterDelegate<Request, Response> callback) where Request : class where Response : class
        {
            String handlerDescription = $"{callback.Method.DeclaringType.Name}.{callback.Method.Name}";

            Logger.LogDebug("Registering request handler for '{Message}' on '{Handler}'...", typeof(Request).Name, handlerDescription);

            if (!_requestHandlers.Exists(x => (x.RegisteredCallback as RequestHandlerCallbackTaskResponseWithTransporterDelegate<Request, Response>) == callback))
            {
                ResonanceRequestHandler handler = new ResonanceRequestHandler();
                handler.HasResponse = true;
                handler.RequestType = typeof(Request);
                handler.RegisteredCallback = callback;
                handler.RegisteredCallbackDescription = handlerDescription;
                handler.ResponseCallback = (request) =>
                {
                    return callback.Invoke(this, request as Request).GetAwaiter().GetResult() as ResonanceActionResult<Response>;
                };

                _requestHandlers.Add(handler);
            }
            else
            {
                Logger.LogWarning("Request handler for '{Message}' on '{Handler}' was already registered.", typeof(Request).Name, handlerDescription);
            }
        }

        /// <summary>
        /// Unregisters a custom request handler.
        /// </summary>
        /// <typeparam name="Request">The type of the request.</typeparam>
        /// <typeparam name="Response">The type of the response.</typeparam>
        /// <param name="callback">The callback method to detach.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public void UnregisterRequestHandler<Request, Response>(RequestHandlerCallbackTaskResponseWithTransporterDelegate<Request, Response> callback) where Request : class where Response : class
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Copies this instance request handlers and registered services to the specified instance.
        /// </summary>
        /// <param name="transporter">The transporter to copy the handlers to.</param>
        public void CopyRequestHandlersAndServices(IResonanceTransporter transporter)
        {
            foreach (var service in _services.ToList())
            {
                (transporter as ResonanceTransporter)._services.Add(service);
                _services.Remove(service);
            }

            foreach (var handler in _requestHandlers.ToList())
            {
                (transporter as ResonanceTransporter)._requestHandlers.Add(handler);
                _requestHandlers.Remove(handler);
            }
        }

        #endregion

        #region Services

        /// <summary>
        /// Registers an instance of <see cref="IResonanceService" /> as a request handler service.
        /// Each method with return type of <see cref="ResonanceActionResult{T}" /> will be registered has a request handler.
        /// Request handler methods should accept only the request as a single parameter.
        /// </summary>
        /// <param name="service">The service.</param>
        public void RegisterService(IResonanceService service)
        {
            if (_services.Contains(service)) throw new InvalidOperationException("The specified service is already registered.");

            Logger.LogDebug($"Registering a request handler service '{service.GetType().FullName}'...");

            List<MethodInfo> methods = new List<MethodInfo>();

            foreach (var method in service.GetType().GetMethods())
            {
                if (typeof(IResonanceActionResult).IsAssignableFrom(method.ReturnType) || (typeof(Task).IsAssignableFrom(method.ReturnType) && method.ReturnType.GenericTypeArguments.Length == 1 && typeof(IResonanceActionResult).IsAssignableFrom(method.ReturnType.GenericTypeArguments[0])))
                {
                    if (method.GetParameters().Length > 1)
                    {
                        throw new InvalidOperationException($"Request handler '{method.Name}' should accept only the request as a parameter.");
                    }

                    if (method.GetParameters().Length == 0)
                    {
                        throw new InvalidOperationException($"Request handler '{method.Name}' does not define any request as a parameter.");
                    }

                    methods.Add(method);
                }
            }

            foreach (var method in methods)
            {
                var requestType = method.GetParameters()[0].ParameterType;

                String handlerDescription = $"{method.DeclaringType.Name}.{method.Name}";

                Logger.LogDebug("Registering request handler for '{Message}' on '{Handler}'...", requestType.Name, handlerDescription);

                ResonanceRequestHandler handler = new ResonanceRequestHandler();
                handler.HasResponse = true;
                handler.RequestType = requestType;
                handler.Service = service;
                handler.RegisteredCallbackDescription = handlerDescription;
                handler.ResponseCallback = (request) =>
                {
                    if (typeof(Task).IsAssignableFrom(method.ReturnType))
                    {
                        var task = (Task)method.Invoke(service, new object[] { request });
                        task.GetAwaiter().GetResult();
                        var prop = typeof(Task<>).MakeGenericType(method.ReturnType.GenericTypeArguments[0]).GetProperty("Result");
                        var value = prop.GetValue(task);
                        return value as IResonanceActionResult;
                    }
                    else
                    {
                        return method.Invoke(service, new object[] { request });
                    }
                };

                _requestHandlers.Add(handler);
            }

            _services.Add(service);
        }

        /// <summary>
        /// Detach the specified <see cref="IResonanceService" /> and all its request handlers.
        /// </summary>
        /// <param name="service">The service.</param>
        public void UnregisterService(IResonanceService service)
        {
            Logger.LogDebug($"Unregistering a request handler service '{service.GetType().FullName}'...");
            _requestHandlers.RemoveAll(x => x.Service == service);
            _services.Remove(service);
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

            try
            {
                Logger.LogInformation("Connecting Transporter...");

                ValidateConnection();

                HandShakeNegotiator.WriteHandShake -= HandShakeNegotiator_WriteHandShake;
                HandShakeNegotiator.SymmetricPasswordAcquired -= HandShakeNegotiator_SymmetricPasswordAcquired;
                HandShakeNegotiator.Completed -= HandShakeNegotiator_Completed;
                HandShakeNegotiator.WriteHandShake += HandShakeNegotiator_WriteHandShake;
                HandShakeNegotiator.SymmetricPasswordAcquired += HandShakeNegotiator_SymmetricPasswordAcquired;
                HandShakeNegotiator.Completed += HandShakeNegotiator_Completed;
                HandShakeNegotiator.Reset(CryptographyConfiguration.Enabled, CryptographyConfiguration.CryptographyProvider);

                await Adapter.Connect();

                State = ResonanceComponentState.Connected;
                Logger.LogInformation("Transporter Connected.");

                StartThreads();

                if (Logger.IsEnabled(LogLevel.Debug))
                {
                    String adapterConfiguration = String.Empty;
                    String transcodingConfiguration = String.Empty;

                    if (Adapter != null)
                    {
                        adapterConfiguration = $"{Adapter.GetType().Name}";
                    }
                    if (Encoder != null && Decoder != null)
                    {
                        transcodingConfiguration = $"{Encoder.GetType().Name} | {Decoder.GetType().Name}";
                    }

                    Logger.LogDebug("Connection Configuration:{Adapter}, {Transcoding}, {@KeepAlive}, {@Cryptography}", adapterConfiguration, transcodingConfiguration, KeepAliveConfiguration, CryptographyConfiguration);
                }
            }
            catch (Exception ex)
            {
                throw Logger.LogErrorThrow(ex, "Error occurred while trying to connect the transporter.");
            }
        }

        /// <summary>
        /// Disconnects this transporter along the underlying <see cref="Adapter"/>.
        /// </summary>
        /// <returns></returns>
        public async Task Disconnect()
        {
            if (State == ResonanceComponentState.Connected)
            {
                try
                {
                    Logger.LogInformation("Disconnecting Transporter...");

                    if (Adapter != null && Adapter.State == ResonanceComponentState.Connected)
                    {
                        if (NotifyOnDisconnect)
                        {
                            try
                            {
                                Logger.LogInformation("Sending disconnection request.");
                                var response = await SendRequest(new ResonanceDisconnectRequest());
                            }
                            catch { }
                        }

                        if (HandShakeNegotiator.State != ResonanceHandShakeState.Idle)
                        {
                            if (HandShakeNegotiator.State != ResonanceHandShakeState.Completed)
                            {
                                Logger.LogInformation("Waiting for handshake completion...");

                                bool cancel = false;

                                TimeoutTask.StartNew(() =>
                                {
                                    cancel = true;
                                    Logger.LogWarning("Could not detect handshake completion within 5 seconds.");
                                }, TimeSpan.FromSeconds(5));

                                while (HandShakeNegotiator.State != ResonanceHandShakeState.Completed && !cancel)
                                {
                                    Thread.Sleep(2);
                                }
                            }
                        }
                    }

                    State = ResonanceComponentState.Disconnected;

                    await FinalizeDisconnection();

                    Logger.LogInformation("Transporter Disconnected.");
                }
                catch (Exception ex)
                {
                    throw Logger.LogErrorThrow(ex, "Error occurred while trying to disconnect the transporter.");
                }
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
            ValidateMessagingState(request);

            config = config ?? new ResonanceRequestConfig();
            config.Timeout = config.Timeout ?? DefaultRequestTimeout;

            TaskCompletionSource<Object> completionSource = new TaskCompletionSource<object>();

            ResonancePendingRequest pendingRequest = new ResonancePendingRequest();
            pendingRequest.Request = request;
            pendingRequest.Config = config;
            pendingRequest.CompletionSource = completionSource;

            String logMessage = "Sending request message: '{Message}'...";

            using (Logger.BeginScopeToken(request.Token))
            {
                switch (config.LoggingMode)
                {
                    case ResonanceMessageLoggingMode.Content:
                        Logger.LogInformation($"{logMessage}{{@Content}}", request.MessageTypeName, request.Message);
                        break;
                    case ResonanceMessageLoggingMode.Title:
                        Logger.LogInformation(logMessage, request.MessageTypeName);
                        break;
                    case ResonanceMessageLoggingMode.None:
                        Logger.LogDebug(logMessage, request.MessageTypeName);
                        break;
                }
            }

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
            ValidateMessagingState(request);

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

            String logMessage = "Sending continuous request message: '{Message}'...";

            using (Logger.BeginScopeToken(resonanceRequest.Token))
            {
                switch (config.LoggingMode)
                {
                    case ResonanceMessageLoggingMode.Content:
                        Logger.LogInformation($"{logMessage}{{@Content}}", resonanceRequest.MessageTypeName, resonanceRequest.Message);
                        break;
                    case ResonanceMessageLoggingMode.Title:
                        Logger.LogInformation(logMessage, resonanceRequest.MessageTypeName);
                        break;
                    case ResonanceMessageLoggingMode.None:
                        Logger.LogDebug(logMessage, resonanceRequest.MessageTypeName);
                        break;
                }
            }

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
            return SendResponse(response, false, config);
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

            return SendResponse(new ResonanceResponse() { Message = message, Token = token }, true, config);
        }

        private Task SendResponse(ResonanceResponse response, bool isError, ResonanceResponseConfig config = null)
        {
            ValidateMessagingState(response);

            config = config ?? new ResonanceResponseConfig();

            TaskCompletionSource<Object> completionSource = new TaskCompletionSource<object>();

            ResonancePendingResponse pendingResponse = new ResonancePendingResponse();
            pendingResponse.Response = response;
            pendingResponse.CompletionSource = completionSource;
            pendingResponse.Config = config;


            String error = response.Message.ToStringOrEmpty().Ellipsis(50);
            String errorMessage = "Sending error response: '{Error}'...";

            String message = "Sending response message: '{Message}'...";

            using (Logger.BeginScopeToken(response.Token))
            {
                if (isError)
                {
                    switch (config.LoggingMode)
                    {
                        case ResonanceMessageLoggingMode.None:
                            Logger.LogDebug(errorMessage, error);
                            break;
                        default:
                            Logger.LogInformation(errorMessage, error);
                            break;
                    }
                }
                else
                {
                    switch (config.LoggingMode)
                    {
                        case ResonanceMessageLoggingMode.Content:
                            Logger.LogInformation($"{message}{{@Content}}", response.MessageTypeName, response.Message);
                            break;
                        case ResonanceMessageLoggingMode.Title:
                            Logger.LogInformation(message, response.MessageTypeName);
                            break;
                        case ResonanceMessageLoggingMode.None:
                            Logger.LogDebug(message, response.MessageTypeName);
                            break;
                    }
                }
            }

            _sendingQueue.BlockEnqueue(pendingResponse, config.Priority);

            return completionSource.Task;
        }

        #endregion

        #region Send Object

        /// <summary>
        /// Sends the specified object without expecting any response.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="config">The configuration.</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException"></exception>
        public Task SendObject(object message, ResonanceRequestConfig config = null)
        {
            ValidateMessagingState(message);

            config = config ?? new ResonanceRequestConfig();
            config.Timeout = config.Timeout ?? DefaultRequestTimeout;

            TaskCompletionSource<Object> completionSource = new TaskCompletionSource<object>();

            String token = TokenGenerator.GenerateToken(message);

            ResonancePendingRequest pendingRequest = new ResonancePendingRequest();
            pendingRequest.Request = new ResonanceRequest() { Message = message, Token = token };
            pendingRequest.Config = config;
            pendingRequest.IsWithoutResponse = true;
            pendingRequest.CompletionSource = completionSource;

            String logMessage = "Sending request message: '{Message}'...";

            using (Logger.BeginScopeToken(pendingRequest.Request.Token))
            {
                switch (config.LoggingMode)
                {
                    case ResonanceMessageLoggingMode.Content:
                        Logger.LogInformation($"{logMessage}{{@Content}}", pendingRequest.Request.MessageTypeName, pendingRequest.Request.Message);
                        break;
                    case ResonanceMessageLoggingMode.Title:
                        Logger.LogInformation(logMessage, pendingRequest.Request.MessageTypeName);
                        break;
                    case ResonanceMessageLoggingMode.None:
                        Logger.LogDebug(logMessage, pendingRequest.Request.MessageTypeName);
                        break;
                }
            }

            _sendingQueue.BlockEnqueue(pendingRequest, config.Priority);

            return completionSource.Task;
        }

        #endregion

        #region Push

        private void PushThreadMethod()
        {
            try
            {
                Logger.LogDebug("Push thread started...");

                while (State == ResonanceComponentState.Connected)
                {
                    Object pending = _sendingQueue.BlockDequeue();
                    if (pending == null || State != ResonanceComponentState.Connected)
                    {
                        Logger.LogDebug("Push thread terminated.");
                        return;
                    }

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
                Logger.LogDebug("Push thread has been aborted.");
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
                if (!pendingRequest.IsWithoutResponse)
                {
                    _pendingRequests.Add(pendingRequest);
                }

                ResonanceEncodingInformation info = new ResonanceEncodingInformation();
                info.Token = pendingRequest.Request.Token;
                info.Message = pendingRequest.Request.Message;

                if (pendingRequest.Request.Message is ResonanceKeepAliveRequest)
                {
                    info.Type = ResonanceTranscodingInformationType.KeepAliveRequest;
                }
                else if (pendingRequest.Request.Message is ResonanceDisconnectRequest)
                {
                    info.Type = ResonanceTranscodingInformationType.Disconnect;
                    pendingRequest.SetResult(true);
                }
                else
                {
                    info.Type = ResonanceTranscodingInformationType.Request;
                }

                if (pendingRequest.Config.CancellationToken != null && !pendingRequest.IsWithoutResponse)
                {
                    Task.Factory.StartNew(() =>
                    {
                        while (!pendingRequest.IsCompleted)
                        {
                            Thread.Sleep(10);

                            if (pendingRequest.Config.CancellationToken.Value.IsCancellationRequested)
                            {
                                Logger.LogDebugToken(pendingRequest.Request.Token, "'{Message}' aborted by cancellation token.", pendingRequest.Request.MessageTypeName);
                                _pendingRequests.Remove(pendingRequest);
                                pendingRequest.SetException(new OperationCanceledException());
                            }
                        }
                    });
                }

                OnEncodeAndWriteData(info);

                if (!pendingRequest.IsWithoutResponse)
                {
                    Task.Delay(pendingRequest.Config.Timeout.Value).ContinueWith((x) =>
                    {
                        if (!pendingRequest.IsCompleted)
                        {
                            Logger.LogWarningToken(pendingRequest.Request.Token, "'{Message}' was not provided with a response within the given period of {Timeout} seconds and has timed out.", pendingRequest.Request.MessageTypeName, pendingRequest.Config.Timeout.Value.TotalSeconds);
                            _pendingRequests.Remove(pendingRequest);
                            pendingRequest.SetException(new TimeoutException($"{pendingRequest.Request.MessageTypeName} was not provided with a response within the given period of {pendingRequest.Config.Timeout.Value.TotalSeconds} seconds and has timed out."));
                        }
                    });
                }

                Task.Factory.StartNew(() =>
                {
                    if (pendingRequest.IsWithoutResponse)
                    {
                        pendingRequest.SetResult(true);
                    }

                    OnRequestSent(pendingRequest.Request);
                });
            }
            catch (Exception ex)
            {
                Logger.LogErrorToken(pendingRequest.Request.Token, ex, "Error occurred while trying to send request '{Message}'.", pendingRequest.Request.MessageTypeName);
                pendingRequest.SetException(ex);
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
                _pendingRequests.Add(pendingContinuousRequest);

                ResonanceEncodingInformation info = new ResonanceEncodingInformation();
                info.Token = pendingContinuousRequest.Request.Token;
                info.Message = pendingContinuousRequest.Request.Message;
                info.Type = ResonanceTranscodingInformationType.ContinuousRequest;

                if (pendingContinuousRequest.Config.CancellationToken != null)
                {
                    Task.Factory.StartNew(() =>
                    {
                        while (!pendingContinuousRequest.IsCompleted)
                        {
                            Thread.Sleep(10);

                            if (pendingContinuousRequest.Config.CancellationToken.Value.IsCancellationRequested)
                            {
                                Logger.LogDebugToken(pendingContinuousRequest.Request.Token, "'{Message}' aborted by cancellation token.", pendingContinuousRequest.Request.MessageTypeName);
                                _pendingRequests.Remove(pendingContinuousRequest);
                                pendingContinuousRequest.OnError(new OperationCanceledException());
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
                            Logger.LogWarningToken(pendingContinuousRequest.Request.Token, "Continuous request '{Message}' was not provided with a response within the given period of {Timeout} seconds and has timed out.", pendingContinuousRequest.Request.MessageTypeName, pendingContinuousRequest.Config.Timeout.Value.Seconds);
                            _pendingRequests.Remove(pendingContinuousRequest);
                            pendingContinuousRequest.OnError(new TimeoutException($"Continuous request '{pendingContinuousRequest.Request.MessageTypeName}' was not provided with a response within the given period of {pendingContinuousRequest.Config.Timeout.Value.Seconds} seconds and has timed out."));
                        }
                    }
                    else
                    {
                        if (pendingContinuousRequest.Config.ContinuousTimeout != null)
                        {
                            Task.Factory.StartNew(async () =>
                            {
                                while (!pendingContinuousRequest.IsCompleted)
                                {
                                    await Task.Delay(pendingContinuousRequest.Config.ContinuousTimeout.Value).ContinueWith((y) =>
                                    {
                                        if (!pendingContinuousRequest.IsCompleted)
                                        {
                                            if (DateTime.Now - pendingContinuousRequest.ContinuousObservable.LastResponseTime > pendingContinuousRequest.Config.ContinuousTimeout.Value)
                                            {
                                                Logger.LogWarningToken(pendingContinuousRequest.Request.Token, "Continuous request '{Message}' had failed to provide a response for a period of {Timeout} seconds and has timed out.", pendingContinuousRequest.Request.MessageTypeName, pendingContinuousRequest.Config.ContinuousTimeout.Value.TotalSeconds);
                                                TimeoutException ex = new TimeoutException($"Continuous request '{pendingContinuousRequest.Request.Message.GetType().Name}' had failed to provide a response for a period of {pendingContinuousRequest.Config.ContinuousTimeout.Value.TotalSeconds} seconds and has timed out.");
                                                OnRequestFailed(pendingContinuousRequest.Request, ex);
                                                pendingContinuousRequest.OnError(ex);
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
                Logger.LogErrorToken(pendingContinuousRequest.Request.Token, ex, "Error occurred while trying to send continuous request '{Message}'.", pendingContinuousRequest.Request.MessageTypeName);
                pendingContinuousRequest.OnError(ex);
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

                if (!pendingResponse.CompletionSource.Task.IsCompleted)
                {
                    pendingResponse.CompletionSource.SetResult(true);
                }

                Task.Factory.StartNew(() =>
                {
                    OnResponseSent(pendingResponse.Response);
                });
            }
            catch (Exception ex)
            {
                Logger.LogErrorToken(pendingResponse.Response.Token, ex, "Error occurred while trying to send response '{Message}'.", pendingResponse.Response.MessageTypeName);
                pendingResponse.SetException(ex);
                OnResponseFailed(pendingResponse.Response, ex);
            }
        }

        /// <summary>
        /// Performs the actual encoding and adapter writing.
        /// </summary>
        /// <param name="info">The information.</param>
        protected virtual void OnEncodeAndWriteData(ResonanceEncodingInformation info)
        {
            if (Encoder != null && Adapter != null)
            {
                if (!_preventHandshake && CryptographyConfiguration.Enabled && HandShakeNegotiator.State != ResonanceHandShakeState.Completed)
                {
                    HandShakeNegotiator.BeginHandShake();
                }

                String typeName = String.Empty;

                if (Logger.IsEnabled(LogLevel.Debug))
                {
                    typeName = info.Message.GetType().Name;
                    Logger.LogDebugToken(info.Token, "Encoding message '{Message}'...", typeName);
                }

                byte[] data = Encoder.Encode(info);

                TotalOutgoingMessages++;

                if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugToken(info.Token, "Writing message № {TotalOutgoingMessages} '{Message}' ({Length})...", TotalOutgoingMessages, typeName, data.ToFriendlyByteSize());

                Adapter.Write(data);
            }
        }

        #endregion

        #region Pull

        private void PullThreadMethod()
        {
            try
            {
                Logger.LogDebug("Pull thread started...");

                while (State == ResonanceComponentState.Connected)
                {
                    byte[] data = _arrivedMessages.BlockDequeue();
                    if (data == null || State != ResonanceComponentState.Connected)
                    {
                        Logger.LogDebug("Pull thread terminated.");
                        return;
                    }

                    try
                    {
                        if (data[0] == 0 && HandShakeNegotiator.State != ResonanceHandShakeState.Completed) //When first byte is zero, must be a Handshake message.
                        {
                            try
                            {
                                HandShakeNegotiator.HandShakeMessageDataReceived(data);
                                continue;
                            }
                            catch (Exception ex)
                            {
                                throw Logger.LogErrorThrow(new ResonanceHandshakeException("Could not initiate a proper handshake.", ex));
                            }
                        }
                        else if (TotalIncomingMessages == 0)
                        {
                            _preventHandshake = true;
                        }

                        TotalIncomingMessages++;

                        ResonanceDecodingInformation info = new ResonanceDecodingInformation();

                        try
                        {
                            if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebug("Incoming message received № {TotalIncomingMessages} ({Length}). Decoding...", TotalIncomingMessages, data.ToFriendlyByteSize());

                            if (Decoder != null)
                            {
                                Decoder.Decode(data, info);
                            }
                            else
                            {
                                Logger.LogWarning("Incoming message received but no Decoder specified!");
                                continue;
                            }
                        }
                        catch (Exception ex)
                        {
                            if (info.Token.IsNotNullOrEmpty())
                            {
                                Logger.LogWarningToken(info.Token, ex, "Error decoding incoming message but token was retrieved. continuing...");
                            }
                            else
                            {
                                Logger.LogCritical(ex, "Error decoding incoming message. Continuing.");
                                continue;
                            }
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
                        else if (info.Type == ResonanceTranscodingInformationType.Disconnect)
                        {
                            OnDisconnectRequestReceived(info);
                        }
                        else
                        {
                            IResonancePendingRequest pending = _pendingRequests.ToList().FirstOrDefault(x => x.Request.Token == info.Token);

                            if (pending != null)
                            {
                                if (info.Type == ResonanceTranscodingInformationType.KeepAliveResponse)
                                {
                                    OnKeepAliveResponseReceived(pending as ResonancePendingRequest, info);
                                }
                                else if (pending is ResonancePendingRequest pendingRequest)
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
                                Logger.LogWarningToken(info.Token, "A response message with no awaiting request was received. Token: {Token}. Ignoring...");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "Unexpected error has occurred while processing an incoming message.");
                    }
                }
            }
            catch (ThreadAbortException)
            {
                Logger.LogDebug("Pull thread has been aborted.");
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
            if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugToken(info.Token, "Incoming request received '{Message}'...", info.Message.GetType().Name);

            ResonanceRequest request = ResonanceRequest.CreateGenericRequest(info.Message.GetType());
            request.Token = info.Token;
            request.Message = info.Message;

            Task.Factory.StartNew(() =>
            {
                var handlers = _requestHandlers.ToList().Where(x => x.RequestType == request.Message.GetType()).ToList();

                foreach (var handler in handlers)
                {
                    if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugToken(info.Token, "Invoking request handler '{Handler}'...", handler.RegisteredCallbackDescription);

                    try
                    {
                        if (handler.HasResponse)
                        {
                            IResonanceActionResult result = handler.ResponseCallback.Invoke(request.Message) as IResonanceActionResult;

                            if (result != null && result.Response != null)
                            {
                                if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugToken(info.Token, "Request handler '{Handler}' completed. Sending response...", handler.RegisteredCallbackDescription);
                                SendResponse(result.Response, request.Token, result.Config).GetAwaiter().GetResult();
                            }
                            else
                            {
                                Logger.LogWarningToken(info.Token, "Request handler '{Handler}' returned with null result.", handler.RegisteredCallbackDescription);
                            }
                        }
                        else
                        {
                            handler.Callback.Invoke(this, request);
                            if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugToken(info.Token, "Request handler '{Handler}' completed.", handler.RegisteredCallbackDescription);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogErrorToken(info.Token, ex, "Request handler '{Handler}' threw an exception. Sending automatic error response...", handler.RegisteredCallbackDescription);
                        try
                        {
                            if (ex.InnerException != null) ex = ex.InnerException;
                            SendErrorResponse(ex, request.Token).GetAwaiter().GetResult();
                        }
                        catch (Exception exx)
                        {
                            Logger.LogErrorToken(info.Token, exx, "Error occurred while trying to send an automatic error response.");
                        }
                    }
                }

                try
                {
                    OnRequestReceived(request);
                }
                catch (Exception ex)
                {
                    Logger.LogErrorToken(info.Token, ex, "Error occurred on request received event for message {Message}", info.Message.GetType().Name);
                }
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

            if (!info.HasDecodingException)
            {
                if (!info.HasError)
                {
                    String responseMessage = "Incoming response received '{Message}'...";

                    switch (pendingRequest.Config.LoggingMode)
                    {
                        case ResonanceMessageLoggingMode.Content:
                            Logger.LogInformationToken(info.Token, $"{responseMessage}{{@Content}}", info.Message.GetType().Name, info.Message);
                            break;
                        case ResonanceMessageLoggingMode.Title:
                            Logger.LogInformationToken(info.Token, responseMessage, info.Message.GetType().Name);
                            break;
                        case ResonanceMessageLoggingMode.None:
                            Logger.LogDebugToken(info.Token, responseMessage, info.Message.GetType().Name);
                            break;
                    }
                }
                else
                {
                    String errorMessage = "Incoming error response received for '{Message}', '{Error}'.";

                    switch (pendingRequest.Config.LoggingMode)
                    {
                        case ResonanceMessageLoggingMode.Content:
                        case ResonanceMessageLoggingMode.Title:
                            Logger.LogInformationToken(info.Token, errorMessage, pendingRequest.Request.MessageTypeName, info.ErrorMessage.Ellipsis(50));
                            break;
                        case ResonanceMessageLoggingMode.None:
                            Logger.LogDebugToken(info.Token, errorMessage, pendingRequest.Request.MessageTypeName, info.ErrorMessage.Ellipsis(50));
                            break;
                    }
                }
            }

            if (info.HasDecodingException && info.Token != null)
            {
                pendingRequest.SetException(info.DecoderException);
            }
            else if (!info.HasError)
            {
                pendingRequest.SetResult(info.Message);

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
                pendingRequest.SetException(new ResonanceResponseException(info.ErrorMessage));
            }
        }

        /// <summary>
        /// Handles incoming continuous response messages.
        /// </summary>
        /// <param name="pendingContinuousRequest">The pending continuous request.</param>
        /// <param name="info">The information.</param>
        protected virtual void OnIncomingContinuousResponse(ResonancePendingContinuousRequest pendingContinuousRequest, ResonanceDecodingInformation info)
        {
            if (!info.HasDecodingException)
            {
                if (!info.HasError)
                {
                    String responseMessage = "Incoming continuous response received '{Message}'...";

                    switch (pendingContinuousRequest.Config.LoggingMode)
                    {
                        case ResonanceMessageLoggingMode.Content:
                            Logger.LogInformationToken(info.Token, $"{responseMessage}{{@Content}}", info.Message.GetType().Name, info.Message);
                            break;
                        case ResonanceMessageLoggingMode.Title:
                            Logger.LogInformationToken(info.Token, responseMessage, info.Message.GetType().Name);
                            break;
                        case ResonanceMessageLoggingMode.None:
                            Logger.LogDebugToken(info.Token, responseMessage, info.Message.GetType().Name);
                            break;
                    }
                }
                else
                {
                    String errorMessage = "Incoming error response received for '{Message}', '{Error}'.";

                    switch (pendingContinuousRequest.Config.LoggingMode)
                    {
                        case ResonanceMessageLoggingMode.Content:
                        case ResonanceMessageLoggingMode.Title:
                            Logger.LogInformationToken(info.Token, errorMessage, pendingContinuousRequest.Request.MessageTypeName, info.ErrorMessage.Ellipsis(50));
                            break;
                        case ResonanceMessageLoggingMode.None:
                            Logger.LogDebugToken(info.Token, errorMessage, pendingContinuousRequest.Request.MessageTypeName, info.ErrorMessage.Ellipsis(50));
                            break;
                    }
                }
            }

            if (info.HasDecodingException && info.Token != null)
            {
                pendingContinuousRequest.OnError(info.DecoderException);
            }
            else if (!info.HasError)
            {
                ResonanceResponse response = new ResonanceResponse();
                response.Token = info.Token;
                response.Message = info.Message;

                if (!info.Completed)
                {
                    pendingContinuousRequest.OnNext(info.Message);
                    OnResponseReceived(response);
                }
                else
                {
                    _pendingRequests.Remove(pendingContinuousRequest);

                    pendingContinuousRequest.OnNext(info.Message);
                    if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebug(info.Token, "Continuous request '{Message}' completed.", pendingContinuousRequest.Request.MessageTypeName);
                    pendingContinuousRequest.OnCompleted();
                    OnResponseReceived(response);
                }
            }
            else
            {
                _pendingRequests.Remove(pendingContinuousRequest);

                if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugToken(info.Token, "Continuous request '{Message}' failed.", pendingContinuousRequest.Request.MessageTypeName);
                pendingContinuousRequest.OnError(new ResonanceResponseException(info.ErrorMessage));
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
                if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugToken(info.Token, "KeepAlive request received. Sending response...");

                try
                {
                    await SendResponse(new ResonanceKeepAliveResponse(), info.Token);
                }
                catch (Exception ex)
                {
                    Logger.LogErrorToken(info.Token, ex, "Error sending keep alive auto response.");
                }
            }
            else
            {
                Logger.LogWarningToken(info.Token, "KeepAlive request received. auto response is disabled...");
            }
        }

        /// <summary>
        /// Called when a keep alive response has been received.
        /// </summary>
        /// <param name="pendingRequest">The pending request.</param>
        /// <param name="info">The information.</param>
        protected virtual void OnKeepAliveResponseReceived(ResonancePendingRequest pendingRequest, ResonanceDecodingInformation info)
        {
            if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugToken(pendingRequest.Request.Token, "KeepAlive response received...");
            _pendingRequests.Remove(pendingRequest);
            pendingRequest.SetResult(info.Message);
        }

        /// <summary>
        /// Called when a <see cref="ResonanceDisconnectRequest"/> has been received.
        /// </summary>
        /// <param name="info">The information.</param>
        /// <exception cref="NotImplementedException"></exception>
        protected virtual void OnDisconnectRequestReceived(ResonanceDecodingInformation info)
        {
            Logger.LogDebug("Disconnect request received. Failing transporter...");
            OnFailed(new ResonanceConnectionClosedException());
        }

        #endregion

        #region KeepAlive

        private void KeepAliveThreadMethod()
        {
            Logger.LogDebug("KeepAlive thread started...");

            _lastIncomingMessageTime = DateTime.Now;

            int retryCounter = 0;

            Thread.Sleep(KeepAliveConfiguration.Delay);

            while (State == ResonanceComponentState.Connected)
            {
                if (KeepAliveConfiguration.Enabled)
                {
                    try
                    {
                        retryCounter++;

                        var response = SendRequest<ResonanceKeepAliveRequest, ResonanceKeepAliveResponse>(new ResonanceKeepAliveRequest(), new ResonanceRequestConfig()
                        {
                            Priority = QueuePriority.Low
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
                                    Logger.LogError(keepAliveException, "The transporter has not received a KeepAlive response within the given time.");
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
                                    Logger.LogDebug($"The transporter has not received a KeepAlive response within the given time. Retrying ({retryCounter}/{KeepAliveConfiguration.Retries})...");
                                }
                            }
                            else
                            {
                                retryCounter = 0;
                                Logger.LogWarning($"The transporter has not received a KeepAlive response within the given time, but was rescued due to other message received within the given time.");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "Error occurred on keep alive mechanism.");
                    }
                }

                Thread.Sleep((int)Math.Max(KeepAliveConfiguration.Interval.TotalMilliseconds, 500));
            }

            Logger.LogDebug("KeepAlive thread terminated.");
        }

        #endregion

        #region HandShake Negotiator Event Handlers

        private void HandShakeNegotiator_WriteHandShake(object sender, ResonanceHandShakeWriteEventArgs e)
        {
            Adapter?.Write(e.Data);
        }

        private void HandShakeNegotiator_SymmetricPasswordAcquired(object sender, ResonanceHandShakeSymmetricPasswordAcquiredEventArgs e)
        {
            Logger.LogDebug($"Symmetric password acquired: {e.SymmetricPassword}");
            Encoder?.EncryptionConfiguration.EnableEncryption(e.SymmetricPassword);
            Decoder?.EncryptionConfiguration.EnableEncryption(e.SymmetricPassword);
        }

        private void HandShakeNegotiator_Completed(object sender, EventArgs e)
        {
            IsChannelSecure = HandShakeNegotiator.State == ResonanceHandShakeState.Completed && Encoder.EncryptionConfiguration.Enabled && Decoder.EncryptionConfiguration.Enabled;

            if (IsChannelSecure)
            {
                Logger.LogInformation("Channel is now secured!");
            }
        }

        #endregion

        #region Start/Stop Threads

        private void StartThreads()
        {
            if (!_clearedQueues)
            {
                ClearQueues();
            }

            Logger.LogDebug("Starting threads...");

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
                Logger.LogDebug("Stopping threads...");

                _sendingQueue.BlockEnqueue(null);
                _arrivedMessages.BlockEnqueue(null);

                if (_pushThread.ThreadState == ThreadState.Running)
                {
                    _pushThread.Join();
                }

                if (_pullThread.ThreadState == ThreadState.Running)
                {
                    _pullThread.Join();
                }

                Logger.LogDebug("Threads terminated...");
            });
        }

        private void ClearQueues()
        {
            Logger.LogDebug("Clearing queues...");
            _sendingQueue = new PriorityProducerConsumerQueue<object>();
            _pendingRequests = new ConcurrentList<IResonancePendingRequest>();
            _arrivedMessages = new ProducerConsumerQueue<byte[]>();
            _clearedQueues = true;
        }

        #endregion

        #region Disconnection Procedures

        /// <summary>
        /// Called when the transporter has failed.
        /// </summary>
        /// <param name="exception">The failed exception.</param>
        /// <param name="failTransporter">Determines whether to disconnect and fail the transporter.</param>
        protected virtual async void OnFailed(Exception exception, bool failTransporter = true)
        {
            if (State != ResonanceComponentState.Failed)
            {
                var args = OnConnectionLost(exception, failTransporter);

                if (args.FailTransporter)
                {
                    FailedStateException = exception;
                    Logger.LogError(exception, "Transporter failed.");
                    State = ResonanceComponentState.Failed;
                    await FinalizeDisconnection(exception);
                }
            }
        }

        /// <summary>
        /// Performs disconnection final procedures.
        /// </summary>
        /// <returns></returns>
        protected virtual async Task FinalizeDisconnection(Exception exception = null)
        {
            Logger.LogDebug("Finalizing disconnection...");

            await StopThreads();

            if (Adapter != null)
            {
                await Adapter.Disconnect();
            }

            NotifyActiveMessagesAboutDisconnection(exception);

            ClearQueues();
        }

        /// <summary>
        /// Notifies all active messages about the transporter disconnection.
        /// </summary>
        protected virtual void NotifyActiveMessagesAboutDisconnection(Exception exception = null)
        {
            if (exception == null) exception = new ResonanceTransporterDisconnectedException("Transporter disconnected.");

            var pendingRequests = _pendingRequests.ToList();

            if (pendingRequests.Count > 0)
            {
                Logger.LogDebug("Aborting all pending request messages...");

                foreach (var pending in pendingRequests)
                {
                    try
                    {
                        _pendingRequests.Remove(pending);

                        if (pending.Request.Message != null)
                        {
                            if (pending.Request.Message is ResonanceDisconnectRequest) continue;
                            if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugToken(pending.Request.Token, "Aborting request '{Message}'...", pending.Request.MessageTypeName);
                        }

                        OnRequestFailed(pending.Request, exception);

                        if (pending is ResonancePendingContinuousRequest continuousPendingRequest)
                        {
                            continuousPendingRequest.OnError(exception);
                        }
                        else if (pending is ResonancePendingRequest pendingRequest)
                        {
                            pendingRequest.SetException(exception);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarningToken(pending.Request.Token, ex, "Error occurred while trying to abort a pending request message.");
                    }
                }
            }

            if (_sendingQueue.Count > 0)
            {
                List<Object> sendingQueue = new List<object>();

                while (_sendingQueue.Count > 0)
                {
                    sendingQueue.Add(_sendingQueue.BlockDequeue());
                }

                Logger.LogDebug("Aborting all pending response messages...");

                foreach (var toSend in sendingQueue)
                {
                    if (toSend is ResonancePendingResponse pendingResponse)
                    {
                        if (pendingResponse.Response.Message != null)
                        {
                            if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugToken(pendingResponse.Response.Token, "Aborting response '{Message}'...", pendingResponse.Response.MessageTypeName);
                        }

                        pendingResponse.SetException(exception);
                    }
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
            RequestReceived?.Invoke(this, new ResonanceRequestReceivedEventArgs(this, request));
        }

        /// <summary>
        /// Called when a request has been sent.
        /// </summary>
        /// <param name="request">The request.</param>
        protected virtual void OnRequestSent(ResonanceRequest request)
        {
            RequestSent?.Invoke(this, new ResonanceRequestEventArgs(this, request));
        }

        /// <summary>
        /// Called when a request has failed.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="exception">The exception.</param>
        protected virtual void OnRequestFailed(ResonanceRequest request, Exception exception)
        {
            RequestFailed?.Invoke(this, new ResonanceRequestFailedEventArgs(this, request, exception));
        }

        /// <summary>
        /// Called when a response has been sent.
        /// </summary>
        /// <param name="response">The response.</param>
        protected virtual void OnResponseSent(ResonanceResponse response)
        {
            ResponseSent?.Invoke(this, new ResonanceResponseEventArgs(this, response));
        }

        /// <summary>
        /// Called when a response has been received.
        /// </summary>
        /// <param name="response">The response.</param>
        protected virtual void OnResponseReceived(ResonanceResponse response)
        {
            ResponseReceived?.Invoke(this, new ResonanceResponseEventArgs(this, response));
        }

        /// <summary>
        /// Called when a response has failed.
        /// </summary>
        /// <param name="response">The response.</param>
        /// <param name="exception">The exception.</param>
        protected virtual void OnResponseFailed(ResonanceResponse response, Exception exception)
        {
            ResponseFailed?.Invoke(this, new ResonanceResponseFailedEventArgs(this, response, exception));
        }

        /// <summary>
        /// Called when when the keep alive mechanism is enabled and has failed.
        /// </summary>
        protected virtual void OnKeepAliveFailed()
        {
            KeepAliveFailed?.Invoke(this, new EventArgs());
        }

        /// <summary>
        /// Called when the Transporter connection has been lost.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <param name="failTransporter">Sets a value indicating whether fail the transporter after this loss of connection</param>
        protected virtual ResonanceConnectionLostEventArgs OnConnectionLost(Exception exception, bool failTransporter)
        {
            var args = new ResonanceConnectionLostEventArgs(exception, failTransporter);
            ConnectionLost?.Invoke(this, args);
            return args;
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
            Logger.LogDebug($"State changed '{previousState}' => '{newState}'.");

            StateChanged?.Invoke(this, new ResonanceComponentStateChangedEventArgs(previousState, newState));

            foreach (var service in _services.ToList())
            {
                try
                {
                    Logger.LogDebug($"Invoking service '{service.GetType().Name}' transporter state changed method.");
                    service.OnTransporterStateChanged(newState);
                }
                catch { }
            }
        }

        /// <summary>
        /// Called when the <see cref="Adapter"/> has changed.
        /// </summary>
        /// <param name="previousAdapter">The previous adapter if any.</param>
        /// <param name="newAdapter">The new adapter.</param>
        protected virtual void OnAdapterChanged(IResonanceAdapter previousAdapter, IResonanceAdapter newAdapter)
        {
            //if (previousAdapter != newAdapter) //Why would I need this ?
            //{
            //    ClearQueues();
            //}

            if (previousAdapter != null)
            {
                previousAdapter.StateChanged -= OnAdapterStateChanged;
                previousAdapter.DataAvailable -= OnAdapterDataAvailable;
            }

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
            return $"{GetType().Name} {_componentCounter}";
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Disconnects and disposes this transporter.
        /// </summary>
        public void Dispose()
        {
            Dispose(false);
        }

        /// <summary>
        /// Disconnects and disposes this transporter.
        /// </summary>
        /// <param name="withAdapter"><c>true</c> to release the underlying <see cref="Adapter"/> along with this transporter.</param>
        public void Dispose(bool withAdapter = false)
        {
            DisposeAsync(withAdapter).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Disconnects and disposes this transporter.
        /// </summary>
        public Task DisposeAsync()
        {
            return DisposeAsync(false);
        }

        /// <summary>
        /// Disconnects and disposes this transporter.
        /// </summary>
        /// <param name="withAdapter"><c>true</c> to release the underlying <see cref="Adapter" /> along with this transporter.</param>
        public async Task DisposeAsync(bool withAdapter)
        {
            if (State != ResonanceComponentState.Disposed && !_isDisposing)
            {
                try
                {
                    Logger.LogInformation("Disposing...");
                    _isDisposing = true;
                    await Disconnect();

                    if (withAdapter)
                    {
                        await Adapter?.DisposeAsync();
                    }

                    Logger.LogInformation("Disposed.");
                    State = ResonanceComponentState.Disposed;
                }
                catch (Exception ex)
                {
                    throw Logger.LogErrorThrow(ex, $"Error occurred while trying to dispose the transporter.");
                }
                finally
                {
                    _isDisposing = false;
                }
            }
        }

        #endregion

        #region Validation

        /// <summary>
        /// Validates the state of the messaging system.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <exception cref="System.NullReferenceException">
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// </exception>
        private void ValidateMessagingState(Object message)
        {
            if (message == null)
                throw Logger.LogErrorThrow(new NullReferenceException("Error processing null message."));

            if (State != ResonanceComponentState.Connected)
                throw Logger.LogErrorThrow(new InvalidOperationException($"Could not send a message while the transporter state is '{State}'."));

            if (Adapter.State != ResonanceComponentState.Connected)
                throw Logger.LogErrorThrow(new InvalidOperationException($"Could not send a message while the adapter state is '{Adapter.State}'."));

            if (Adapter == null)
                throw Logger.LogErrorThrow(new NullReferenceException($"No Adapter specified. Could not send a message."));

            if (Encoder == null)
                throw Logger.LogErrorThrow(new NullReferenceException($"No Encoder specified. Could not send a message."));

            if (Decoder == null)
                throw Logger.LogErrorThrow(new NullReferenceException($"No Decoder specified. Could not send a message."));

            if (TokenGenerator == null)
                throw Logger.LogErrorThrow(new NullReferenceException($"No Token Generator specified. Could not send a message."));
        }

        /// <summary>
        /// Validates the state of the transporter for connection.
        /// </summary>
        private void ValidateConnection()
        {
            Logger.LogDebug($"Validating connection state...");

            if (Adapter == null)
                throw Logger.LogErrorThrow(new NullReferenceException($"Please specify an Adapter before attempting to connect."));

            if (Encoder == null)
                throw Logger.LogErrorThrow(new NullReferenceException($"Please specify an Encoder before attempting to connect."));

            if (Decoder == null)
                throw Logger.LogErrorThrow(new NullReferenceException($"Please specify a Decoder before attempting to connect."));

            if (TokenGenerator == null)
                throw Logger.LogErrorThrow(new NullReferenceException($"Please specify a Token Generator before attempting to connect."));

            if (HandShakeNegotiator == null)
                throw Logger.LogErrorThrow(new NullReferenceException($"Please specify a Handshake Negotiator before attempting to connect."));

            if (CryptographyConfiguration == null)
                throw Logger.LogErrorThrow(new NullReferenceException($"Please specify a cryptography configuration before attempting to connect."));
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
