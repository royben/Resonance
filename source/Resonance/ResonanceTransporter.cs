﻿using Microsoft.Extensions.Logging;
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
    /// Represents the <see cref="IResonanceTransporter"/> primary implementation.
    /// </summary>
    /// <seealso cref="Resonance.IResonanceTransporter" />
    public class ResonanceTransporter : ResonanceObject, IResonanceTransporter
    {
        private readonly int _componentCounter;
        private DateTime _lastIncomingMessageTime;
        private PriorityProducerConsumerQueue<Object> _sendingQueue;
        private ConcurrentList<IResonancePendingMessage> _pendingMessages;
        private ProducerConsumerQueue<byte[]> _arrivedMessages;
        private readonly List<ResonanceMessageHandler> _messageHandlers;
        private readonly List<IResonanceService> _services;
        private Thread _pushThread;
        private Thread _pullThread;
        private Thread _keepAliveThread;
        private bool _clearedQueues;
        private bool _isDisposing;
        private bool _preventHandshake;

        #region Events

        /// <summary>
        /// Occurs when a new message has been received.
        /// </summary>
        public event EventHandler<ResonanceMessageReceivedEventArgs> MessageReceived;

        /// <summary>
        /// Occurs when a message has been sent.
        /// </summary>
        public event EventHandler<ResonanceMessageEventArgs> MessageSent;

        /// <summary>
        /// Occurs when a sent message has failed.
        /// </summary>
        public event EventHandler<ResonanceMessageFailedEventArgs> MessageFailed;

        /// <summary>
        /// Occurs when a new request message has been received.
        /// </summary>
        public event EventHandler<ResonanceMessageReceivedEventArgs> RequestReceived;

        /// <summary>
        /// Occurs when a request has been sent.
        /// </summary>
        public event EventHandler<ResonanceMessageEventArgs> RequestSent;

        /// <summary>
        /// Occurs when a request has failed.
        /// </summary>
        public event EventHandler<ResonanceMessageFailedEventArgs> RequestFailed;

        /// <summary>
        /// Occurs when a response has been sent.
        /// </summary>
        public event EventHandler<ResonanceMessageEventArgs> ResponseSent;

        /// <summary>
        /// Occurs when a request response has been received.
        /// </summary>
        public event EventHandler<ResonanceMessageEventArgs> ResponseReceived;

        /// <summary>
        /// Occurs when a response has failed to be sent.
        /// </summary>
        public event EventHandler<ResonanceMessageFailedEventArgs> ResponseFailed;

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
        /// Gets or sets the message acknowledgment behavior when receiving and sending standard messages.
        /// </summary>
        public ResonanceMessageAckBehavior MessageAcknowledgmentBehavior { get; set; }

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
        /// Gets the number of current pending outgoing messages.
        /// </summary>
        public int TotalPendingOutgoingMessages
        {
            get
            {
                return _pendingMessages.Count;
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

            _messageHandlers = new List<ResonanceMessageHandler>();
            _services = new List<IResonanceService>();

            _lastIncomingMessageTime = DateTime.Now;

            NotifyOnDisconnect = true;

            KeepAliveConfiguration = ResonanceGlobalSettings.Default.DefaultKeepAliveConfiguration();
            CryptographyConfiguration = new ResonanceCryptographyConfiguration(); //Should be set by GlobalSettings.
            TokenGenerator = new ShortGuidGenerator();
            _sendingQueue = new PriorityProducerConsumerQueue<object>();
            _pendingMessages = new ConcurrentList<IResonancePendingMessage>();
            _arrivedMessages = new ProducerConsumerQueue<byte[]>();

            DefaultRequestTimeout = ResonanceGlobalSettings.Default.DefaultRequestTimeout;

            FailsWithAdapter = true;
        }

        #endregion

        #region Request Handlers

        /// <summary>
        /// Registers a custom message handler.
        /// </summary>
        /// <typeparam name="Message">The type of the message.</typeparam>
        /// <param name="callback">The callback method to register.</param>
        public void RegisterMessageHandler<Message>(MessageHandlerCallbackDelegate<Message> callback) where Message : class
        {
            String handlerDescription = $"{callback.Method.DeclaringType.Name}.{callback.Method.Name}";

            Logger.LogDebug("Registering message handler for '{Message}' on '{Handler}'...", typeof(Message).Name, handlerDescription);

            if (!_messageHandlers.Exists(x => (x.RegisteredCallback as MessageHandlerCallbackDelegate<Message>) == callback))
            {
                ResonanceMessageHandler handler = new ResonanceMessageHandler();
                handler.MessageType = typeof(Message);
                handler.RegisteredCallback = callback;
                handler.RegisteredCallbackDescription = handlerDescription;
                handler.Callback = (transporter, resonanceRequest) =>
                {
                    callback?.Invoke(transporter, resonanceRequest as ResonanceMessage<Message>);
                };

                _messageHandlers.Add(handler);
            }
            else
            {
                Logger.LogWarning("Message handler for '{Message}' on '{Handler}' was already registered.", typeof(Message).Name, handlerDescription);
            }
        }

        /// <summary>
        /// Unregisters a custom message handler.
        /// </summary>
        /// <typeparam name="Message">The type of the message.</typeparam>
        /// <param name="callback">The callback method to detach.</param>
        public void UnregisterMessageHandler<Message>(MessageHandlerCallbackDelegate<Message> callback) where Message : class
        {
            Logger.LogDebug("Unregistering message handler for '{Message}' on '{Handler}'...", typeof(Message).Name, $"{callback.Method.DeclaringType}.{callback.Method.Name}");

            var handler = _messageHandlers.FirstOrDefault(x => (x.RegisteredCallback as MessageHandlerCallbackDelegate<Message>) == callback);
            if (handler != null)
            {
                _messageHandlers.Remove(handler);
            }
        }

        /// <summary>
        /// Registers a custom request handler.
        /// </summary>
        /// <typeparam name="Request">The type of the request.</typeparam>
        /// <param name="callback">The callback method to register.</param>
        public void RegisterRequestHandler<Request>(MessageHandlerCallbackDelegate<Request> callback) where Request : class
        {
            String handlerDescription = $"{callback.Method.DeclaringType.Name}.{callback.Method.Name}";

            Logger.LogDebug("Registering request handler for '{Message}' on '{Handler}'...", typeof(Request).Name, handlerDescription);

            if (!_messageHandlers.Exists(x => (x.RegisteredCallback as MessageHandlerCallbackDelegate<Request>) == callback))
            {
                ResonanceMessageHandler handler = new ResonanceMessageHandler();
                handler.MessageType = typeof(Request);
                handler.RegisteredCallback = callback;
                handler.RegisteredCallbackDescription = handlerDescription;
                handler.Callback = (transporter, resonanceRequest) =>
                {
                    callback?.Invoke(transporter, resonanceRequest as ResonanceMessage<Request>);
                };

                _messageHandlers.Add(handler);
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
        public void UnregisterRequestHandler<Request>(MessageHandlerCallbackDelegate<Request> callback) where Request : class
        {
            Logger.LogDebug("Unregistering request handler for '{Message}' on '{Handler}'...", typeof(Request).Name, $"{callback.Method.DeclaringType}.{callback.Method.Name}");

            var handler = _messageHandlers.FirstOrDefault(x => (x.RegisteredCallback as MessageHandlerCallbackDelegate<Request>) == callback);
            if (handler != null)
            {
                _messageHandlers.Remove(handler);
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

            if (!_messageHandlers.Exists(x => (x.RegisteredCallback as RequestHandlerCallbackDelegate<Request, Response>) == callback))
            {
                ResonanceMessageHandler handler = new ResonanceMessageHandler();
                handler.HasResponse = true;
                handler.MessageType = typeof(Request);
                handler.RegisteredCallback = callback;
                handler.RegisteredCallbackDescription = handlerDescription;
                handler.ResponseCallback = (request) =>
                {
                    return (ResonanceActionResult<Response>)callback.Invoke(request as Request);
                };

                _messageHandlers.Add(handler);
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

            var handler = _messageHandlers.FirstOrDefault(x => (x.RegisteredCallback as RequestHandlerCallbackDelegate<Request, Response>) == callback);
            if (handler != null)
            {
                _messageHandlers.Remove(handler);
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

            if (!_messageHandlers.Exists(x => (x.RegisteredCallback as RequestHandlerResponseWithTransporterCallbackDelegate<Request, Response>) == callback))
            {
                ResonanceMessageHandler handler = new ResonanceMessageHandler();
                handler.HasResponse = true;
                handler.MessageType = typeof(Request);
                handler.RegisteredCallback = callback;
                handler.RegisteredCallbackDescription = handlerDescription;
                handler.ResponseCallback = (request) =>
                {
                    return (ResonanceActionResult<Response>)callback.Invoke(this, request as Request);
                };

                _messageHandlers.Add(handler);
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

            var handler = _messageHandlers.FirstOrDefault(x => (x.RegisteredCallback as RequestHandlerResponseWithTransporterCallbackDelegate<Request, Response>) == callback);
            if (handler != null)
            {
                _messageHandlers.Remove(handler);
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

            if (!_messageHandlers.Exists(x => (x.RegisteredCallback as RequestHandlerCallbackTaskResponseWithTransporterDelegate<Request, Response>) == callback))
            {
                ResonanceMessageHandler handler = new ResonanceMessageHandler();
                handler.HasResponse = true;
                handler.MessageType = typeof(Request);
                handler.RegisteredCallback = callback;
                handler.RegisteredCallbackDescription = handlerDescription;
                handler.ResponseCallback = (request) =>
                {
                    return callback.Invoke(this, request as Request).GetAwaiter().GetResult() as ResonanceActionResult<Response>;
                };

                _messageHandlers.Add(handler);
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

            foreach (var handler in _messageHandlers.ToList())
            {
                (transporter as ResonanceTransporter)._messageHandlers.Add(handler);
                _messageHandlers.Remove(handler);
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
                if ( //With Response
                    (typeof(IResonanceActionResult).IsAssignableFrom(method.ReturnType) || 
                    (typeof(Task).IsAssignableFrom(method.ReturnType) && 
                    method.ReturnType.GenericTypeArguments.Length == 1 && 
                    typeof(IResonanceActionResult).IsAssignableFrom(method.ReturnType.GenericTypeArguments[0]))) ||

                    //void Or Task
                    method.ReturnType == typeof(void) || method.ReturnType == typeof(Task))
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

                ResonanceMessageHandler handler = new ResonanceMessageHandler();
                handler.MessageType = requestType;
                handler.Service = service;
                handler.RegisteredCallbackDescription = handlerDescription;

                if (method.ReturnType == typeof(void) || method.ReturnType == typeof(Task))
                {
                    handler.Callback = (transporter, resonanceRequest) =>
                    {
                        if (method.ReturnType == typeof(Task))
                        {
                            var task = (Task)method.Invoke(service, new object[] { (resonanceRequest as ResonanceMessage).Object });
                            task.GetAwaiter().GetResult();
                        }
                        else
                        {
                            method.Invoke(service, new object[] { (resonanceRequest as ResonanceMessage).Object });
                        }
                    };
                }
                else
                {
                    handler.HasResponse = true;

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
                }

                _messageHandlers.Add(handler);
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
            _messageHandlers.RemoveAll(x => x.Service == service);
            _services.Remove(service);
        }

        #endregion

        #region Connect/Disconnect

        /// <summary>
        /// Connects this transporter along with the underlying <see cref="Adapter"/>.
        /// </summary>
        /// <returns></returns>
        public async Task ConnectAsync()
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

                await Adapter.ConnectAsync();

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
        /// Connects this transporter along with the underlying <see cref="Adapter"/>.
        /// </summary>
        /// <returns></returns>
        public void Connect()
        {
            ConnectAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Disconnects this transporter along the underlying <see cref="Adapter"/>.
        /// </summary>
        /// <returns></returns>
        public async Task DisconnectAsync()
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
                                await SendAsync(new ResonanceDisconnectNotification());
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

        /// <summary>
        /// Disconnects this transporter along the underlying <see cref="Adapter"/>.
        /// </summary>
        /// <returns></returns>
        public void Disconnect()
        {
            DisconnectAsync().GetAwaiter().GetResult();
        }

        #endregion

        #region Send Message

        /// <summary>
        /// Sends the specified object without expecting any response.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="config">The configuration.</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException"></exception>
        public Task SendAsync(object message, ResonanceMessageConfig config = null)
        {
            ValidateMessagingState(message);
            return SendAsync(new ResonanceMessage() { Object = message, Token = TokenGenerator.GenerateToken(message) }, config);
        }

        /// <summary>
        /// Sends the specified object without expecting any response.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="config">The configuration.</param>
        public void Send(Object message, ResonanceMessageConfig config = null)
        {
            SendAsync(message, config).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Sends the specified object without expecting any response.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="config">The configuration.</param>
        /// <returns></returns>
        public Task SendAsync(ResonanceMessage message, ResonanceMessageConfig config = null)
        {
            ValidateMessagingState(message);

            config = config ?? new ResonanceMessageConfig();
            config.Timeout = config.Timeout ?? DefaultRequestTimeout;

            TaskCompletionSource<Object> completionSource = new TaskCompletionSource<object>();

            ResonancePendingMessage pendingMessage = new ResonancePendingMessage();
            pendingMessage.Message = message;
            pendingMessage.Config = config;
            pendingMessage.CompletionSource = completionSource;

            String logMessage = String.Empty;

            if (!config.RequireACK)
            {
                logMessage = "Sending message: '{Message}'...";
            }
            else
            {
                logMessage = "Sending message: '{Message}', ACK required...";
            }

            using (Logger.BeginScopeToken(pendingMessage.Message.Token))
            {
                switch (config.LoggingMode)
                {
                    case ResonanceMessageLoggingMode.Content:
                        Logger.LogInformation($"{logMessage}{{@Content}}", pendingMessage.Message.ObjectTypeName, pendingMessage.Message.Object);
                        break;
                    case ResonanceMessageLoggingMode.Title:
                        Logger.LogInformation(logMessage, pendingMessage.Message.ObjectTypeName);
                        break;
                    case ResonanceMessageLoggingMode.None:
                        Logger.LogDebug(logMessage, pendingMessage.Message.ObjectTypeName);
                        break;
                }
            }

            _sendingQueue.BlockEnqueue(pendingMessage, config.Priority);

            return completionSource.Task;
        }

        /// <summary>
        /// Sends the specified object without expecting any response.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="config">The configuration.</param>
        public void Send(ResonanceMessage message, ResonanceMessageConfig config = null)
        {
            SendAsync(message, config).GetAwaiter().GetResult();
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
        public async Task<Response> SendRequestAsync<Request, Response>(Request request, ResonanceRequestConfig config = null)
        {
            return (Response)await SendRequestAsync(request, config);
        }

        /// <summary>
        /// Sends the specified request message and returns a response.
        /// </summary>
        /// <typeparam name="Request">The type of the Request.</typeparam>
        /// <typeparam name="Response">The type of the Response.</typeparam>
        /// <param name="request">The request message.</param>
        /// <param name="config">Request configuration.</param>
        /// <returns></returns>
        public Response SendRequest<Request, Response>(Request request, ResonanceRequestConfig config = null)
        {
            return SendRequestAsync<Request, Response>(request, config).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Sends the specified request message and returns a response.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="config">The configuration.</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException"></exception>
        public Task<Object> SendRequestAsync(Object request, ResonanceRequestConfig config = null)
        {
            ResonanceMessage resonanceRequest = new ResonanceMessage();
            resonanceRequest.Token = TokenGenerator.GenerateToken(request);
            resonanceRequest.Object = request;

            return SendRequestAsync(resonanceRequest, config);
        }

        /// <summary>
        /// Sends the specified request message and returns a response.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="config">The configuration.</param>
        /// <returns></returns>
        public Object SendRequest(Object request, ResonanceRequestConfig config = null)
        {
            return SendRequestAsync(request, config).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Sends the specified request message and returns a response.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="config">The configuration.</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException"></exception>
        public Task<Object> SendRequestAsync(ResonanceMessage request, ResonanceRequestConfig config = null)
        {
            ValidateMessagingState(request);

            config = config ?? new ResonanceRequestConfig();
            config.Timeout = config.Timeout ?? DefaultRequestTimeout;

            TaskCompletionSource<Object> completionSource = new TaskCompletionSource<object>();

            ResonancePendingRequest pendingRequest = new ResonancePendingRequest();
            pendingRequest.Message = request;
            pendingRequest.Config = config;
            pendingRequest.CompletionSource = completionSource;

            String logMessage = "Sending request message: '{Message}'...";

            using (Logger.BeginScopeToken(request.Token))
            {
                switch (config.LoggingMode)
                {
                    case ResonanceMessageLoggingMode.Content:
                        Logger.LogInformation($"{logMessage}{{@Content}}", request.ObjectTypeName, request.Object);
                        break;
                    case ResonanceMessageLoggingMode.Title:
                        Logger.LogInformation(logMessage, request.ObjectTypeName);
                        break;
                    case ResonanceMessageLoggingMode.None:
                        Logger.LogDebug(logMessage, request.ObjectTypeName);
                        break;
                }
            }

            _sendingQueue.BlockEnqueue(pendingRequest, config.Priority);

            return completionSource.Task;
        }

        /// <summary>
        /// Sends the specified request message and returns a response.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="config">The configuration.</param>
        /// <returns></returns>
        public Object SendRequest(ResonanceMessage request, ResonanceRequestConfig config = null)
        {
            return SendRequestAsync(request, config).GetAwaiter().GetResult();
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

            ResonanceMessage resonanceRequest = new ResonanceMessage();
            resonanceRequest.Token = TokenGenerator.GenerateToken(request);
            resonanceRequest.Object = request;

            ResonanceObservable<Response> observable = new ResonanceObservable<Response>();

            ResonancePendingContinuousRequest pendingContinuousRequest = new ResonancePendingContinuousRequest();
            pendingContinuousRequest.Message = resonanceRequest;
            pendingContinuousRequest.Config = config;
            pendingContinuousRequest.ContinuousObservable = observable;

            String logMessage = "Sending continuous request message: '{Message}'...";

            using (Logger.BeginScopeToken(resonanceRequest.Token))
            {
                switch (config.LoggingMode)
                {
                    case ResonanceMessageLoggingMode.Content:
                        Logger.LogInformation($"{logMessage}{{@Content}}", resonanceRequest.ObjectTypeName, resonanceRequest.Object);
                        break;
                    case ResonanceMessageLoggingMode.Title:
                        Logger.LogInformation(logMessage, resonanceRequest.ObjectTypeName);
                        break;
                    case ResonanceMessageLoggingMode.None:
                        Logger.LogDebug(logMessage, resonanceRequest.ObjectTypeName);
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
        public Task SendResponseAsync<Response>(ResonanceMessage<Response> response, ResonanceResponseConfig config = null)
        {
            return SendResponseAsync((ResonanceMessage)response, config);
        }

        /// <summary>
        /// Sends a response message.
        /// </summary>
        /// <typeparam name="Response">The type of the Response.</typeparam>
        /// <param name="response">The response message.</param>
        /// <param name="config">Response configuration.</param>
        /// <returns></returns>
        public void SendResponse<Response>(ResonanceMessage<Response> response, ResonanceResponseConfig config = null)
        {
            SendResponseAsync<Response>(response, config).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Sends the specified response message.
        /// </summary>
        /// <param name="message">The response message.</param>
        /// <param name="token">Request token.</param>
        /// <param name="config">Response configuration.</param>
        /// <returns></returns>
        public Task SendResponseAsync(Object message, String token, ResonanceResponseConfig config = null)
        {
            return SendResponseAsync(new ResonanceMessage()
            {
                Object = message,
                Token = token
            }, config);
        }

        /// <summary>
        /// Sends the specified response message.
        /// </summary>
        /// <param name="message">The response message.</param>
        /// <param name="token">Request token.</param>
        /// <param name="config">Response configuration.</param>
        /// <returns></returns>
        public void SendResponse(Object message, String token, ResonanceResponseConfig config = null)
        {
            SendResponseAsync(message, token, config).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Sends the specified response message.
        /// </summary>
        /// <param name="response">The response message.</param>
        /// <param name="config">Response configuration.</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException"></exception>
        public Task SendResponseAsync(ResonanceMessage response, ResonanceResponseConfig config = null)
        {
            return SendResponse(response, false, config);
        }

        /// <summary>
        /// Sends the specified response message.
        /// </summary>
        /// <param name="response">The response message.</param>
        /// <param name="config">Response configuration.</param>
        /// <returns></returns>
        public void SendResponse(ResonanceMessage response, ResonanceResponseConfig config = null)
        {
            SendResponseAsync(response, config).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Sends a general error response agnostic to the type of request.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <param name="token">The request token.</param>
        /// <returns></returns>
        public Task SendErrorResponseAsync(Exception exception, string token)
        {
            return SendErrorResponseAsync(exception.Message, token);
        }

        /// <summary>
        /// Sends a general error response agnostic to the type of request.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <param name="token">The request token.</param>
        /// <returns></returns>
        public void SendErrorResponse(Exception exception, String token)
        {
            SendErrorResponseAsync(exception, token).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Sends a general error response agnostic to the type of request.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="token">The request token.</param>
        /// <returns></returns>
        public Task SendErrorResponseAsync(String message, string token)
        {
            ResonanceResponseConfig config = new ResonanceResponseConfig();
            config.HasError = true;
            config.ErrorMessage = message;

            return SendResponse(new ResonanceMessage() { Object = message, Token = token }, true, config);
        }

        /// <summary>
        /// Sends a general error response agnostic to the type of request.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="token">The request token.</param>
        /// <returns></returns>
        public void SendErrorResponse(String message, string token)
        {
            SendErrorResponseAsync(message, token).GetAwaiter().GetResult();
        }

        private Task SendResponse(ResonanceMessage response, bool isError, ResonanceResponseConfig config = null)
        {
            ValidateMessagingState(response);

            config = config ?? new ResonanceResponseConfig();

            TaskCompletionSource<Object> completionSource = new TaskCompletionSource<object>();

            ResonancePendingResponse pendingResponse = new ResonancePendingResponse();
            pendingResponse.Response = response;
            pendingResponse.CompletionSource = completionSource;
            pendingResponse.Config = config;


            String error = response.Object.ToStringOrEmpty().Ellipsis(50);
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
                            Logger.LogInformation($"{message}{{@Content}}", response.ObjectTypeName, response.Object);
                            break;
                        case ResonanceMessageLoggingMode.Title:
                            Logger.LogInformation(message, response.ObjectTypeName);
                            break;
                        case ResonanceMessageLoggingMode.None:
                            Logger.LogDebug(message, response.ObjectTypeName);
                            break;
                    }
                }
            }

            _sendingQueue.BlockEnqueue(pendingResponse, config.Priority);

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

                    if (pending is ResonancePendingMessage pendingMessage)
                    {
                        OnOutgoingMessage(pendingMessage);
                    }
                    else if (pending is ResonancePendingRequest pendingRequest)
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

        protected virtual void OnOutgoingMessage(ResonancePendingMessage pendingMessage)
        {
            try
            {
                if (pendingMessage.Config.RequireACK)
                {
                    _pendingMessages.Add(pendingMessage);
                }

                ResonanceEncodingInformation info = new ResonanceEncodingInformation();
                info.Token = pendingMessage.Message.Token;
                info.Message = pendingMessage.Message.Object;

                if (pendingMessage.Message.Object is ResonanceKeepAliveRequest)
                {
                    info.Type = ResonanceTranscodingInformationType.KeepAliveRequest;
                }
                else if (pendingMessage.Message.Object is ResonanceDisconnectNotification)
                {
                    info.Type = ResonanceTranscodingInformationType.Disconnect;
                }
                else if (pendingMessage.Message.Object is ResonanceAcknowledgeMessage ackMessage)
                {
                    info.Type = ResonanceTranscodingInformationType.MessageSyncACK;

                    if (ackMessage.Exception != null)
                    {
                        info.HasError = true;
                        info.ErrorMessage = ackMessage.Exception.Message;
                    }

                    try
                    {
                        OnEncodeAndWriteData(info);
                        pendingMessage.SetResult();
                    }
                    catch { /*TODO: Minimum logging*/ }
                    return;
                }
                else if (pendingMessage.Config.RequireACK)
                {
                    info.Type = ResonanceTranscodingInformationType.MessageSync;
                }
                else
                {
                    info.Type = ResonanceTranscodingInformationType.Message;
                }

                if (pendingMessage.Config.CancellationToken != null && pendingMessage.Config.RequireACK)
                {
                    Task.Factory.StartNew(() =>
                    {
                        while (!pendingMessage.IsCompleted)
                        {
                            Thread.Sleep(10);

                            if (pendingMessage.Config.CancellationToken.Value.IsCancellationRequested)
                            {
                                Logger.LogDebugToken(pendingMessage.Message.Token, "'{Message}' aborted by cancellation token.", pendingMessage.Message.ObjectTypeName);
                                _pendingMessages.Remove(pendingMessage);
                                pendingMessage.SetException(new OperationCanceledException());
                            }
                        }
                    });
                }

                OnEncodeAndWriteData(info);

                if (pendingMessage.Config.RequireACK)
                {
                    Task.Delay(pendingMessage.Config.Timeout.Value).ContinueWith((x) =>
                    {
                        if (!pendingMessage.IsCompleted)
                        {
                            Logger.LogWarningToken(pendingMessage.Message.Token, "'{Message}' was not provided with an acknowledgment within the given period of {Timeout} seconds and has timed out.", pendingMessage.Message.ObjectTypeName, pendingMessage.Config.Timeout.Value.TotalSeconds);
                            _pendingMessages.Remove(pendingMessage);

                            var timeoutException = new TimeoutException($"{pendingMessage.Message.ObjectTypeName} was not provided with an acknowledgment within the given period of {pendingMessage.Config.Timeout.Value.TotalSeconds} seconds and has timed out.");
                            OnMessageFailed(pendingMessage.Message, timeoutException);
                            pendingMessage.SetException(timeoutException);
                        }
                    });
                }

                Task.Factory.StartNew(() =>
                {
                    if (!pendingMessage.Config.RequireACK)
                    {
                        pendingMessage.SetResult();
                    }

                    OnMessageSent(pendingMessage.Message);
                });
            }
            catch (Exception ex)
            {
                Logger.LogErrorToken(pendingMessage.Message.Token, ex, "Error occurred while trying to send message '{Message}'.", pendingMessage.Message.ObjectTypeName);
                OnMessageFailed(pendingMessage.Message, ex);
                pendingMessage.SetException(ex);
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
                _pendingMessages.Add(pendingRequest);

                ResonanceEncodingInformation info = new ResonanceEncodingInformation();
                info.Token = pendingRequest.Message.Token;
                info.Message = pendingRequest.Message.Object;

                if (pendingRequest.Message.Object is ResonanceKeepAliveRequest)
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
                        while (!pendingRequest.IsCompleted)
                        {
                            Thread.Sleep(10);

                            if (pendingRequest.Config.CancellationToken.Value.IsCancellationRequested)
                            {
                                Logger.LogDebugToken(pendingRequest.Message.Token, "'{Message}' aborted by cancellation token.", pendingRequest.Message.ObjectTypeName);
                                _pendingMessages.Remove(pendingRequest);
                                pendingRequest.SetException(new OperationCanceledException());
                            }
                        }
                    });
                }

                OnEncodeAndWriteData(info);

                Task.Delay(pendingRequest.Config.Timeout.Value).ContinueWith((x) =>
                {
                    if (!pendingRequest.IsCompleted)
                    {
                        Logger.LogWarningToken(pendingRequest.Message.Token, "'{Message}' was not provided with a response within the given period of {Timeout} seconds and has timed out.", pendingRequest.Message.ObjectTypeName, pendingRequest.Config.Timeout.Value.TotalSeconds);
                        _pendingMessages.Remove(pendingRequest);

                        var timeoutException = new TimeoutException($"{pendingRequest.Message.ObjectTypeName} was not provided with a response within the given period of {pendingRequest.Config.Timeout.Value.TotalSeconds} seconds and has timed out.");
                        OnRequestFailed(pendingRequest.Message, timeoutException);
                        pendingRequest.SetException(timeoutException);
                    }
                });

                Task.Factory.StartNew(() =>
                {
                    OnRequestSent(pendingRequest.Message);
                });
            }
            catch (Exception ex)
            {
                Logger.LogErrorToken(pendingRequest.Message.Token, ex, "Error occurred while trying to send request '{Message}'.", pendingRequest.Message.ObjectTypeName);
                OnRequestFailed(pendingRequest.Message, ex);
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
                _pendingMessages.Add(pendingContinuousRequest);

                ResonanceEncodingInformation info = new ResonanceEncodingInformation();
                info.Token = pendingContinuousRequest.Message.Token;
                info.Message = pendingContinuousRequest.Message.Object;
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
                                Logger.LogDebugToken(pendingContinuousRequest.Message.Token, "'{Message}' aborted by cancellation token.", pendingContinuousRequest.Message.ObjectTypeName);
                                _pendingMessages.Remove(pendingContinuousRequest);
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
                            Logger.LogWarningToken(pendingContinuousRequest.Message.Token, "Continuous request '{Message}' was not provided with a response within the given period of {Timeout} seconds and has timed out.", pendingContinuousRequest.Message.ObjectTypeName, pendingContinuousRequest.Config.Timeout.Value.Seconds);
                            _pendingMessages.Remove(pendingContinuousRequest);
                            var timeoutException = new TimeoutException($"Continuous request '{pendingContinuousRequest.Message.ObjectTypeName}' was not provided with a response within the given period of {pendingContinuousRequest.Config.Timeout.Value.Seconds} seconds and has timed out.");
                            OnRequestFailed(pendingContinuousRequest.Message, timeoutException);
                            pendingContinuousRequest.OnError(timeoutException);
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
                                                Logger.LogWarningToken(pendingContinuousRequest.Message.Token, "Continuous request '{Message}' had failed to provide a response for a period of {Timeout} seconds and has timed out.", pendingContinuousRequest.Message.ObjectTypeName, pendingContinuousRequest.Config.ContinuousTimeout.Value.TotalSeconds);
                                                TimeoutException ex = new TimeoutException($"Continuous request '{pendingContinuousRequest.Message.Object.GetType().Name}' had failed to provide a response for a period of {pendingContinuousRequest.Config.ContinuousTimeout.Value.TotalSeconds} seconds and has timed out.");
                                                OnRequestFailed(pendingContinuousRequest.Message, ex);
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
                    OnRequestSent(pendingContinuousRequest.Message);
                });
            }
            catch (Exception ex)
            {
                Logger.LogErrorToken(pendingContinuousRequest.Message.Token, ex, "Error occurred while trying to send continuous request '{Message}'.", pendingContinuousRequest.Message.ObjectTypeName);
                OnRequestFailed(pendingContinuousRequest.Message, ex);
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
                info.Message = pendingResponse.Response.Object;
                info.Completed = pendingResponse.Config.Completed;
                info.ErrorMessage = pendingResponse.Config.ErrorMessage;
                info.HasError = pendingResponse.Config.HasError;

                if (pendingResponse.Response.Object is ResonanceKeepAliveResponse)
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
                Logger.LogErrorToken(pendingResponse.Response.Token, ex, "Error occurred while trying to send response '{Message}'.", pendingResponse.Response.ObjectTypeName);
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

                        if (info.Type == ResonanceTranscodingInformationType.Message || info.Type == ResonanceTranscodingInformationType.MessageSync)
                        {
                            if (!info.HasDecodingException)
                            {
                                OnIncomingMessage(info);
                            }
                        }
                        else if (info.Type == ResonanceTranscodingInformationType.MessageSyncACK)
                        {
                            if (!info.HasDecodingException)
                            {
                                OnIncomingMessageACK(info);
                            }
                        }
                        else if (info.Type == ResonanceTranscodingInformationType.Request || info.Type == ResonanceTranscodingInformationType.ContinuousRequest)
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
                            OnDisconnectNotificationReceived(info);
                        }
                        else
                        {
                            IResonancePendingMessage pending = _pendingMessages.ToList().FirstOrDefault(x => x.Message.Token == info.Token);

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
        /// Handles incoming messages.
        /// </summary>
        /// <param name="info">The information.</param>
        protected virtual void OnIncomingMessage(ResonanceDecodingInformation info)
        {
            if (info.Type == ResonanceTranscodingInformationType.MessageSync)
            {
                if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugToken(info.Token, "Incoming message received '{Message}', ACK required...", info.Message.GetType().Name);

                if (MessageAcknowledgmentBehavior == ResonanceMessageAckBehavior.Default)
                {
                    try
                    {
                        Send(new ResonanceMessage() { Object = new ResonanceAcknowledgeMessage(), Token = info.Token },new ResonanceMessageConfig() { Priority = QueuePriority.High });
                    }
                    catch { }
                }
            }
            else
            {
                if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugToken(info.Token, "Incoming message received '{Message}'...", info.Message.GetType().Name);
            }

            Exception exception = null;

            ResonanceMessage request = ResonanceMessage.CreateGenericMessage(info.Message.GetType());
            request.Token = info.Token;
            request.Object = info.Message;

            Task.Factory.StartNew(() =>
            {
                var handlers = _messageHandlers.ToList().Where(x => !x.HasResponse && x.MessageType == request.Object.GetType()).ToList();

                foreach (var handler in handlers)
                {
                    if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugToken(info.Token, "Invoking message handler '{Handler}'...", handler.RegisteredCallbackDescription);

                    try
                    {
                        handler.Callback.Invoke(this, request);
                        if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugToken(info.Token, "Message handler '{Handler}' completed.", handler.RegisteredCallbackDescription);
                    }
                    catch (Exception ex)
                    {
                        exception = ex;
                        Logger.LogErrorToken(info.Token, ex, "Message handler '{Handler}' threw an exception.", handler.RegisteredCallbackDescription);
                    }
                }

                try
                {
                    OnMessageReceived(request);
                }
                catch (Exception ex)
                {
                    exception = ex;
                    Logger.LogErrorToken(info.Token, ex, "Error occurred on message received event for message {Message}", info.Message.GetType().Name);
                }

                if (info.Type == ResonanceTranscodingInformationType.MessageSync && MessageAcknowledgmentBehavior == ResonanceMessageAckBehavior.ReportErrors)
                {
                    try
                    {
                        Send(new ResonanceMessage() { Object = new ResonanceAcknowledgeMessage() { Exception = exception }, Token = info.Token });
                    }
                    catch { }
                }
            });
        }

        /// <summary>
        /// Handles incoming ACK messages.
        /// </summary>
        /// <param name="info">The information.</param>
        protected virtual void OnIncomingMessageACK(ResonanceDecodingInformation info)
        {
            ResonancePendingMessage pending = _pendingMessages.ToList().FirstOrDefault(x => x.Message.Token == info.Token) as ResonancePendingMessage;

            if (pending != null)
            {
                _pendingMessages.Remove(pending);

                if (!info.HasError || MessageAcknowledgmentBehavior == ResonanceMessageAckBehavior.Default)
                {
                    if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugToken(info.Token, "Incoming message ACK received for message '{Message}'...", pending.Message.ObjectTypeName);
                    pending.SetResult();
                }
                else
                {
                    if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugToken(info.Token, "Incoming message ACK received with error '{Error}' for message '{Message}'...", info.ErrorMessage, pending.Message.ObjectTypeName);
                    pending.SetException(new ResonanceResponseException(info.ErrorMessage));
                }
            }
            else
            {
                Logger.LogWarningToken(info.Token, "A message ACK with no awaiting message was received. Token: {Token}. Ignoring...");
            }
        }

        /// <summary>
        /// Handles incoming request messages.
        /// </summary>
        /// <param name="info">The information.</param>
        protected virtual void OnIncomingRequest(ResonanceDecodingInformation info)
        {
            if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugToken(info.Token, "Incoming request received '{Message}'...", info.Message.GetType().Name);

            ResonanceMessage request = ResonanceMessage.CreateGenericMessage(info.Message.GetType());
            request.Token = info.Token;
            request.Object = info.Message;

            Task.Factory.StartNew(() =>
            {
                var handlers = _messageHandlers.ToList().Where(x => x.MessageType == request.Object.GetType()).ToList();

                foreach (var handler in handlers)
                {
                    if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugToken(info.Token, "Invoking request handler '{Handler}'...", handler.RegisteredCallbackDescription);

                    try
                    {
                        if (handler.HasResponse)
                        {
                            IResonanceActionResult result = handler.ResponseCallback.Invoke(request.Object) as IResonanceActionResult;

                            if (result != null && result.Response != null)
                            {
                                if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugToken(info.Token, "Request handler '{Handler}' completed. Sending response...", handler.RegisteredCallbackDescription);
                                SendResponse(result.Response, request.Token, result.Config);
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
                            SendErrorResponse(ex, request.Token);
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
            _pendingMessages.Remove(pendingRequest);

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
                            Logger.LogInformationToken(info.Token, errorMessage, pendingRequest.Message.ObjectTypeName, info.ErrorMessage.Ellipsis(50));
                            break;
                        case ResonanceMessageLoggingMode.None:
                            Logger.LogDebugToken(info.Token, errorMessage, pendingRequest.Message.ObjectTypeName, info.ErrorMessage.Ellipsis(50));
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

                Task.Factory.StartNew((Action)(() =>
                {
                    ResonanceMessage response = new ResonanceMessage();
                    response.Token = info.Token;
                    response.Object = info.Message;
                    OnResponseReceived(response);
                }));
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
                            Logger.LogInformationToken(info.Token, errorMessage, pendingContinuousRequest.Message.ObjectTypeName, info.ErrorMessage.Ellipsis(50));
                            break;
                        case ResonanceMessageLoggingMode.None:
                            Logger.LogDebugToken(info.Token, errorMessage, pendingContinuousRequest.Message.ObjectTypeName, info.ErrorMessage.Ellipsis(50));
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
                ResonanceMessage response = new ResonanceMessage();
                response.Token = info.Token;
                response.Object = info.Message;

                if (!info.Completed)
                {
                    pendingContinuousRequest.OnNext(info.Message);
                    OnResponseReceived(response);
                }
                else
                {
                    _pendingMessages.Remove(pendingContinuousRequest);

                    pendingContinuousRequest.OnNext(info.Message);
                    if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebug(info.Token, "Continuous request '{Message}' completed.", pendingContinuousRequest.Message.ObjectTypeName);
                    pendingContinuousRequest.OnCompleted();
                    OnResponseReceived(response);
                }
            }
            else
            {
                _pendingMessages.Remove(pendingContinuousRequest);

                if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugToken(info.Token, "Continuous request '{Message}' failed.", pendingContinuousRequest.Message.ObjectTypeName);
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
                    await SendResponseAsync(new ResonanceKeepAliveResponse(), info.Token);
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
            if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugToken(pendingRequest.Message.Token, "KeepAlive response received...");
            _pendingMessages.Remove(pendingRequest);
            pendingRequest.SetResult(info.Message);
        }

        /// <summary>
        /// Called when a <see cref="ResonanceDisconnectNotification"/> has been received.
        /// </summary>
        /// <param name="info">The information.</param>
        protected virtual void OnDisconnectNotificationReceived(ResonanceDecodingInformation info)
        {
            Logger.LogDebug("Disconnection notification received. Failing transporter...");
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
                        });

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
            _pendingMessages = new ConcurrentList<IResonancePendingMessage>();
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
                await Adapter.DisconnectAsync();
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

            var pendingRequests = _pendingMessages.ToList();

            if (pendingRequests.Count > 0)
            {
                Logger.LogDebug("Aborting all pending request messages...");

                foreach (var pending in pendingRequests)
                {
                    try
                    {
                        _pendingMessages.Remove(pending);

                        if (pending.Message.Object != null)
                        {
                            if (pending.Message.Object is ResonanceDisconnectNotification) continue;
                            if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugToken(pending.Message.Token, "Aborting request '{Message}'...", pending.Message.ObjectTypeName);
                        }

                        OnRequestFailed(pending.Message, exception);

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
                        Logger.LogWarningToken(pending.Message.Token, ex, "Error occurred while trying to abort a pending request message.");
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
                        if (pendingResponse.Response.Object != null)
                        {
                            if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugToken(pendingResponse.Response.Token, "Aborting response '{Message}'...", pendingResponse.Response.ObjectTypeName);
                        }

                        pendingResponse.SetException(exception);
                    }
                }
            }
        }

        #endregion

        #region Events Notification Methods

        /// <summary>
        /// Called when a message has been received.
        /// </summary>
        /// <param name="message">The resonance message.</param>
        protected virtual void OnMessageReceived(ResonanceMessage message)
        {
            MessageReceived?.Invoke(this, new ResonanceMessageReceivedEventArgs(this, message));
        }

        /// <summary>
        /// Called when a message has been sent.
        /// </summary>
        /// <param name="message">The resonance message.</param>
        protected virtual void OnMessageSent(ResonanceMessage message)
        {
            MessageSent?.Invoke(this, new ResonanceMessageEventArgs(this, message));
        }

        /// <summary>
        /// Called when a sent message has failed.
        /// </summary>
        /// <param name="message">The resonance message.</param>
        /// <param name="exception">The exception.</param>
        protected virtual void OnMessageFailed(ResonanceMessage message, Exception exception)
        {
            MessageFailed?.Invoke(this, new ResonanceMessageFailedEventArgs(this, message, exception));
        }

        /// <summary>
        /// Called when a request has been received.
        /// </summary>
        /// <param name="request">The request.</param>
        protected virtual void OnRequestReceived(ResonanceMessage request)
        {
            RequestReceived?.Invoke(this, new ResonanceMessageReceivedEventArgs(this, request));
        }

        /// <summary>
        /// Called when a request has been sent.
        /// </summary>
        /// <param name="request">The request.</param>
        protected virtual void OnRequestSent(ResonanceMessage request)
        {
            RequestSent?.Invoke(this, new ResonanceMessageEventArgs(this, request));
        }

        /// <summary>
        /// Called when a request has failed.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="exception">The exception.</param>
        protected virtual void OnRequestFailed(ResonanceMessage request, Exception exception)
        {
            RequestFailed?.Invoke(this, new ResonanceMessageFailedEventArgs(this, request, exception));
        }

        /// <summary>
        /// Called when a response has been sent.
        /// </summary>
        /// <param name="response">The response.</param>
        protected virtual void OnResponseSent(ResonanceMessage response)
        {
            ResponseSent?.Invoke(this, new ResonanceMessageEventArgs(this, response));
        }

        /// <summary>
        /// Called when a response has been received.
        /// </summary>
        /// <param name="response">The response.</param>
        protected virtual void OnResponseReceived(ResonanceMessage response)
        {
            ResponseReceived?.Invoke(this, new ResonanceMessageEventArgs(this, response));
        }

        /// <summary>
        /// Called when a response has failed.
        /// </summary>
        /// <param name="response">The response.</param>
        /// <param name="exception">The exception.</param>
        protected virtual void OnResponseFailed(ResonanceMessage response, Exception exception)
        {
            ResponseFailed?.Invoke(this, new ResonanceMessageFailedEventArgs(this, response, exception));
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
                    await DisconnectAsync();

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
