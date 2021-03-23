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

namespace Resonance
{
    public abstract class ResonanceTransporter : IResonanceTransporter
    {
        private static int _transporterCounter;
        private object _disposeLock = new object();

        private PriorityProducerConsumerQueue<Object> _sendingQueue;
        private ConcurrentList<IResonancePendingRequest> _pendingRequests;
        private ProducerConsumerQueue<byte[]> _arrivedMessages;
        private Thread _pushThread;
        private Thread _pullThread;

        #region Events

        public event EventHandler<ResonanceRequestReceivedEventArgs> RequestReceived;
        public event EventHandler<ResonanceResponse> PendingResponseReceived;
        public event EventHandler<ResonanceRequest> RequestSent;
        public event EventHandler<ResonanceResponse> ResponseSent;
        public event EventHandler<ResonanceResponse> ResponseReceived;
        public event EventHandler<ResonanceRequestFailedEventArgs> RequestFailed;
        public event EventHandler<ResonanceComponentState> StateChanged;

        #endregion

        #region Properties

        private IResonanceAdapter _adapter;
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

        public IResonanceEncoder Encoder { get; set; }
        public IResonanceDecoder Decoder { get; set; }

        private ResonanceComponentState state;
        public ResonanceComponentState State
        {
            get { return state; }
            set
            {
                state = value;
                if (state != value)
                {
                    OnStateChanged();
                }
            }
        }

        public IResonanceTokenGenerator TokenGenerator { get; set; }
        public Exception FailedStateException { get; private set; }
        public TimeSpan RequestTimeout { get; set; }
        public bool UseKeepAlive { get; set; }
        public TimeSpan KeepAliveTimeout { get; set; }
        public int KeepAliveRetries { get; set; }
        public bool EnableKeepAliveAutoResponse { get; set; }
        public bool FailsWithAdapter { get; set; }
        public LogManager LogManager { get; private set; }

        #endregion

        #region Constructors

        public ResonanceTransporter()
        {
            _transporterCounter++;

            TokenGenerator = new GuidTokenGenerator();

            LogManager = LogManager.Default;
            _sendingQueue = new PriorityProducerConsumerQueue<object>();
            _pendingRequests = new ConcurrentList<IResonancePendingRequest>();
            _arrivedMessages = new ProducerConsumerQueue<byte[]>();

            RequestTimeout = TimeSpan.FromSeconds(5);
            EnableKeepAliveAutoResponse = true;
            KeepAliveTimeout = TimeSpan.FromSeconds(2);
            KeepAliveRetries = 1;
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

        public async Task Disconnect()
        {
            if (State == ResonanceComponentState.Connected)
            {
                State = ResonanceComponentState.Disconnected;

                await OnPostDisconnection();

                LogManager.Log($"{this}: Transporter Disconnected...");
            }
        }

        #endregion

        #region Send Request

        public async Task<Response> SendRequest<Request, Response>(Request request, ResonanceRequestConfig config = null)
        {
            return (Response)await SendRequest(request, config);
        }

        public Task<Object> SendRequest(Object request, ResonanceRequestConfig config = null)
        {
            if (State != ResonanceComponentState.Connected)
            {
                throw LogManager.Log(new InvalidOperationException($"{this}: Could not send the request while transporter state is {State}."));
            }

            TaskCompletionSource<Object> completionSource = new TaskCompletionSource<object>();

            config = config ?? new ResonanceRequestConfig();
            config.Timeout = config.Timeout ?? RequestTimeout;

            ResonanceRequest resonanceRequest = new ResonanceRequest();
            resonanceRequest.Token = TokenGenerator.Generate(request);
            resonanceRequest.Message = request;

            ResonancePendingRequest pendingRequest = new ResonancePendingRequest();
            pendingRequest.Request = resonanceRequest;
            pendingRequest.Config = config;
            pendingRequest.CompletionSource = completionSource;

            LogManager.Log($"{this}: Queuing request message: {request.GetType().Name} Token: {resonanceRequest.Token}", LogLevel.Debug);

            _sendingQueue.BlockEnqueue(pendingRequest, config.Priority);

            return completionSource.Task;
        }

        public ResonanceObservable<Response> SendContinuousRequest<Request, Response>(Request request, ResonanceContinuousRequestConfig config = null)
        {
            if (State != ResonanceComponentState.Connected)
            {
                throw LogManager.Log(new InvalidOperationException($"{this}: Could not send the request while transporter state is {State}."));
            }

            config = config ?? new ResonanceContinuousRequestConfig();
            config.Timeout = config.Timeout ?? RequestTimeout;
            config.ContinuousTimeout = config.ContinuousTimeout ?? RequestTimeout;

            ResonanceRequest resonanceRequest = new ResonanceRequest();
            resonanceRequest.Token = TokenGenerator.Generate(request);
            resonanceRequest.Message = request;

            ResonanceObservable<Response> observable = new ResonanceObservable<Response>();

            ResonancePendingContinuousRequest pendingContinuousRequest = new ResonancePendingContinuousRequest();
            pendingContinuousRequest.Request = resonanceRequest;
            pendingContinuousRequest.Config = config;
            pendingContinuousRequest.ContinuousObservable = observable;
            pendingContinuousRequest.ContinuousTimeout = config.ContinuousTimeout;

            LogManager.Log($"{this}: Queuing continuous request message: {request.GetType().Name} Token: {resonanceRequest.Token}", LogLevel.Debug);

            _sendingQueue.BlockEnqueue(pendingContinuousRequest, config.Priority);

            return observable;
        }

        #endregion

        #region Send Response

        public Task SendResponse<Response>(ResonanceResponse<Response> response, ResonanceResponseConfig config = null)
        {
            return SendResponse((ResonanceResponse)response, config);
        }

        public Task SendResponse(Object message, String token, ResonanceResponseConfig config = null)
        {
            return SendResponse(new ResonanceResponse()
            {
                Message = message,
                Token = token
            }, config);
        }

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

        public Task SendErrorResponse(Exception exception, string token)
        {
            return SendErrorResponse(exception.Message, token);
        }

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
                    if (pending == null) return;

                    if (pending is ResonancePendingRequest pendingRequest)
                    {
                        PushRequest(pendingRequest);
                    }
                    else if (pending is ResonancePendingContinuousRequest pendingContinuousRequest)
                    {
                        PushContinuousRequest(pendingContinuousRequest);
                    }
                    else if (pending is ResonancePendingResponse pendingResponse)
                    {
                        PushResponse(pendingResponse);
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

        private void PushRequest(ResonancePendingRequest pendingRequest)
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
                info.IsRequest = true;

                if (pendingRequest.Config.CancellationToken != null)
                {
                    Task.Factory.StartNew(() =>
                    {
                        pendingRequest.Config.CancellationToken.WaitHandle.WaitOne(pendingRequest.Config.Timeout.Value);

                        if (pendingRequest.Config.CancellationToken.IsCancellationRequested)
                        {
                            _pendingRequests.Remove(pendingRequest);
                            pendingRequest.CompletionSource.SetException(new OperationCanceledException());
                        }
                    });
                }

                byte[] data = Encoder.Encode(info);
                Adapter.Write(data);

                Task.Delay(pendingRequest.Config.Timeout.Value, pendingRequest.Config.CancellationToken).ContinueWith((x) =>
                {
                    if (!pendingRequest.CompletionSource.Task.IsCompleted)
                    {
                        if (pendingRequest.Config.CancellationToken == null || !pendingRequest.Config.CancellationToken.IsCancellationRequested)
                        {
                            _pendingRequests.Remove(pendingRequest);
                            pendingRequest.CompletionSource.SetException(new TimeoutException($"{pendingRequest.Request.Message.GetType()} was not provided with a response within the given period of {pendingRequest.Config.Timeout.Value.Seconds} seconds and has timed out."));
                        }
                    }
                });

                OnRequestSent(pendingRequest.Request);
            }
            catch (Exception ex)
            {
                pendingRequest.CompletionSource.SetException(ex);
            }
        }

        private void PushContinuousRequest(ResonancePendingContinuousRequest pendingContinuousRequest)
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
                info.IsRequest = true;

                if (pendingContinuousRequest.Config.CancellationToken != null)
                {
                    Task.Factory.StartNew(() =>
                    {
                        pendingContinuousRequest.Config.CancellationToken.WaitHandle.WaitOne(pendingContinuousRequest.Config.Timeout.Value);

                        if (pendingContinuousRequest.Config.CancellationToken.IsCancellationRequested)
                        {
                            _pendingRequests.Remove(pendingContinuousRequest);
                            pendingContinuousRequest.ContinuousObservable.OnError(new OperationCanceledException());
                        }
                    });
                }

                byte[] data = Encoder.Encode(info);
                Adapter.Write(data);

                Task.Delay(pendingContinuousRequest.Config.Timeout.Value).ContinueWith((x) =>
                {
                    if (!pendingContinuousRequest.ContinuousObservable.FirstMessageArrived)
                    {
                        if (pendingContinuousRequest.Config.CancellationToken == null || !pendingContinuousRequest.Config.CancellationToken.IsCancellationRequested)
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

                OnRequestSent(pendingContinuousRequest.Request);
            }
            catch (Exception ex)
            {
                pendingContinuousRequest.ContinuousObservable.OnError(ex);
            }
        }

        private void PushResponse(ResonancePendingResponse pendingResponse)
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

                byte[] data = Encoder.Encode(info);
                Adapter.Write(data);

                pendingResponse.CompletionSource.SetResult(true);

                OnResponseSent(pendingResponse.Response);
            }
            catch (Exception ex)
            {
                pendingResponse.CompletionSource.SetException(ex);
            }
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
                    if (data == null) return;

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

                        if (info.IsRequest)
                        {
                            if (!info.HasDecodingException)
                            {
                                HandleIncomingRequest(info);
                            }
                        }
                        else
                        {
                            IResonancePendingRequest pending = _pendingRequests.ToList().FirstOrDefault(x => x.Request.Token == info.Token);

                            if (pending != null)
                            {
                                if (pending is ResonancePendingRequest pendingRequest)
                                {
                                    HandleIncomingResponse(pendingRequest, info);
                                }
                                else if (pending is ResonancePendingContinuousRequest pendingContinuousRequest)
                                {
                                    HandleIncomingContinuousResponse(pendingContinuousRequest, info);
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

        private void HandleIncomingRequest(ResonanceDecodingInformation info)
        {
            ResonanceRequest request = new ResonanceRequest();
            request.Token = info.Token;
            request.Message = info.Message;

            Task.Factory.StartNew(() =>
            {
                OnRequestReceived(request);
            });
        }

        private void HandleIncomingResponse(ResonancePendingRequest pendingRequest, ResonanceDecodingInformation info)
        {
            _pendingRequests.Remove(pendingRequest);

            if (info.HasDecodingException && info.Token != null)
            {
                pendingRequest.CompletionSource.SetException(info.DecoderException);
            }
            else if (!info.HasError)
            {
                pendingRequest.CompletionSource.SetResult(info.Message);
            }
            else
            {
                pendingRequest.CompletionSource.SetException(new ResonanceResponseException(info.ErrorMessage));
            }
        }

        private void HandleIncomingContinuousResponse(ResonancePendingContinuousRequest pendingContinuousRequest, ResonanceDecodingInformation info)
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
                if (!info.Completed)
                {
                    Task.Factory.StartNew(() =>
                    {
                        pendingContinuousRequest.ContinuousObservable.OnNext(info.Message);
                    });
                }
                else
                {
                    _pendingRequests.Remove(pendingContinuousRequest);

                    Task.Factory.StartNew(() =>
                    {
                        pendingContinuousRequest.ContinuousObservable.OnNext(info.Message);
                        pendingContinuousRequest.ContinuousObservable.OnCompleted();
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

        #region Public Methods

        #endregion

        #region Virtual Methods

        protected virtual async void OnFailed(Exception ex)
        {
            if (State != ResonanceComponentState.Failed)
            {
                FailedStateException = ex;
                LogManager.Log(ex, $"{this}: Transporter failed.");
                State = ResonanceComponentState.Failed;

                await OnPostDisconnection();
            }
            else
            {
                LogManager.Log(ex, LogLevel.Warning, $"{this}: OnFailed called while state is already failed!");
            }
        }

        protected virtual async Task OnPostDisconnection()
        {
            await StopThreads();

            if (Adapter != null)
            {
                await Adapter.Disconnect();
            }

            NotifyActiveMessagesAboutDisconnection();
        }

        /// <summary>
        /// Notifies all the continuous request messages about disconnection.
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

        private void OnRequestSent(ResonanceRequest request)
        {
            RequestSent?.Invoke(this, request);
        }

        private void OnRequestFailed(ResonanceRequest request, Exception ex)
        {
            RequestFailed?.Invoke(this, new ResonanceRequestFailedEventArgs(request, ex));
        }

        private void OnResponseSent(ResonanceResponse response)
        {
            ResponseSent?.Invoke(this, response);
        }

        private void OnRequestReceived(ResonanceRequest request)
        {
            RequestReceived?.Invoke(this, new ResonanceRequestReceivedEventArgs()
            {
                Request = request,
            });
        }

        #endregion

        #region Property Changes

        private void OnStateChanged()
        {
            StateChanged?.Invoke(this, State);
        }

        private void OnAdapterChanged(IResonanceAdapter oldAdapter, IResonanceAdapter newAdapter)
        {
            if (oldAdapter != newAdapter)
            {
                _pendingRequests.Clear();
                _arrivedMessages = new ProducerConsumerQueue<byte[]>();
                _sendingQueue = new PriorityProducerConsumerQueue<object>();
            }

            if (oldAdapter != null)
            {
                oldAdapter.StateChanged -= OnAdapterStateChanged;
                oldAdapter.DataAvailable -= OnAdapterDataAvailable;
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

        protected virtual void OnAdapterDataAvailable(object sender, byte[] data)
        {
            _arrivedMessages.BlockEnqueue(data);
        }

        protected virtual void OnAdapterStateChanged(object sender, ResonanceComponentState state)
        {
            if (state == ResonanceComponentState.Failed && FailsWithAdapter)
            {
                OnFailed(new ResonanceAdapterFailedException($"The adapter has failed with exception '{Adapter.FailedStateException.Message}' and the transporter is configured to fail with the adapter.", Adapter.FailedStateException));
            }
        }

        #endregion

        #region Override Methods

        public override string ToString()
        {
            return $"Transporter {_transporterCounter} => {Encoder} / {Decoder} => {Adapter}";
        }

        #endregion

        #region IDisposable

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

        public void Dispose(bool withAdapter = false)
        {
            Dispose();

            if (withAdapter)
            {
                Adapter.Dispose();
            }
        }

        #endregion
    }
}
