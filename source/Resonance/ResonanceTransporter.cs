using Resonance.ExtensionMethods;
using Resonance.Logging;
using Resonance.Reactive;
using Resonance.Threading;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Resonance
{
    public class ResonanceTransporter : IResonanceTransporter
    {
        private PriorityProducerConsumerQueue<Object> _sendingQueue;
        private ConcurrentList<IResonanceRequestHandler> _pendingRequests;
        private Thread _pushThread;
        private Thread _pullThread;

        public event EventHandler<ResonanceRequestReceivedEventArgs> RequestReceived;
        public event EventHandler<ResonanceResponse> PendingResponseReceived;
        public event EventHandler<ResonanceRequest> RequestSent;
        public event EventHandler<ResonanceResponse> ResponseSent;
        public event EventHandler<ResonanceResponse> ResponseReceived;
        public event EventHandler<ResonanceRequestFailedEventArgs> RequestFailed;

        public IResonanceAdapter Adapter { get; set; }
        public IResonanceEncoder Encoder { get; set; }
        public IResonanceDecoder Decoder { get; set; }
        public ResonanceComponentState State { get; private set; }
        public IResonanceTokenGenerator TokenGenerator { get; set; }
        public Exception FailedStateException { get; private set; }
        public TimeSpan RequestTimeout { get; set; }
        public bool UseKeepAlive { get; set; }
        public TimeSpan KeepAliveTimeout { get; set; }
        public int KeepAliveRetries { get; set; }
        public bool EnableKeepAliveAutoResponse { get; set; }
        public bool FailsWithAdapter { get; set; }
        public string ComponentName { get; set; }
        public LogManager LogManager { get; private set; }

        #region Constructors

        public ResonanceTransporter()
        {
            LogManager = LogManager.Default;
            _sendingQueue = new PriorityProducerConsumerQueue<object>();
            _pendingRequests = new ConcurrentList<IResonanceRequestHandler>();
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

        public Task Connect()
        {
            throw new NotImplementedException();
        }

        public Task Disconnect()
        {
            throw new NotImplementedException();
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
                throw LogManager.Log(new InvalidOperationException($"{GetExtendedComponentName()}: Could not send the request while transporter state is {State}."));
            }

            TaskCompletionSource<Object> completionSource = new TaskCompletionSource<object>();

            config = config ?? new ResonanceRequestConfig();
            config.Timeout = config.Timeout ?? RequestTimeout;

            ResonanceRequest resonanceRequest = new ResonanceRequest();
            resonanceRequest.Token = TokenGenerator.Generate(request);
            resonanceRequest.Message = request;

            ResonanceRequestHandler handler = new ResonanceRequestHandler();
            handler.Request = resonanceRequest;
            handler.Config = config;
            handler.CompletionSource = completionSource;

            LogManager.Log($"{GetExtendedComponentName()}: Queuing request message: {request.GetType().Name} Token: {resonanceRequest.Token}", LogLevel.Debug);

            _sendingQueue.BlockEnqueue(handler, config.Priority);

            return completionSource.Task;
        }

        public IObservable<Response> SendContinuousRequest<Request, Response>(Request request, ResonanceContinuousRequestConfig config = null)
        {
            if (State != ResonanceComponentState.Connected)
            {
                throw LogManager.Log(new InvalidOperationException($"{GetExtendedComponentName()}: Could not send the request while transporter state is {State}."));
            }

            config = config ?? new ResonanceContinuousRequestConfig();
            config.Timeout = config.Timeout ?? RequestTimeout;
            config.ContinuousTimeout = config.ContinuousTimeout ?? RequestTimeout;

            ResonanceRequest resonanceRequest = new ResonanceRequest();
            resonanceRequest.Token = TokenGenerator.Generate(request);
            resonanceRequest.Message = request;

            ResonanceObservable<Response> observable = new ResonanceObservable<Response>();

            ResonanceContinuousRequestHandler handler = new ResonanceContinuousRequestHandler();
            handler.Request = resonanceRequest;
            handler.Config = config;
            handler.ContinuousObservable = observable;
            handler.ContinuousTimeout = config.ContinuousTimeout;

            LogManager.Log($"{GetExtendedComponentName()}: Queuing continuous request message: {request.GetType().Name} Token: {resonanceRequest.Token}", LogLevel.Debug);

            _sendingQueue.BlockEnqueue(handler, config.Priority);

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
                throw LogManager.Log(new InvalidOperationException($"{GetExtendedComponentName()}: Could not send the response while transporter state is {State}."));
            }

            TaskCompletionSource<Object> completionSource = new TaskCompletionSource<object>();

            ResonanceResponseHandler handler = new ResonanceResponseHandler();
            handler.Response = response;
            handler.CompletionSource = completionSource;
            handler.Config = config;
            _sendingQueue.BlockEnqueue(handler, config.Priority);

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

        public void ClearQueues()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        protected virtual String GetExtendedComponentName()
        {
            return Adapter != null ? $"{ComponentName} ({Adapter.Address})" : ComponentName;
        }

        private void StartThreads()
        {
            _pullThread = new Thread(PullThreadMethod);
            _pullThread.Name = $"{GetExtendedComponentName()} Pull Thread";
            _pullThread.IsBackground = true;
            _pullThread.Start();

            _pushThread = new Thread(PushThreadMethod);
            _pushThread.Name = $"{GetExtendedComponentName()} Push Thread";
            _pushThread.IsBackground = true;
            _pushThread.Start();
        }

        #region Push

        private void PushThreadMethod()
        {
            try
            {
                while (State == ResonanceComponentState.Connected)
                {
                    Object handler = _sendingQueue.BlockDequeue();

                    if (handler is ResonanceRequestHandler requestHandler)
                    {
                        PushRequest(requestHandler);
                    }
                    else if (handler is ResonanceContinuousRequestHandler continuousHandler)
                    {
                        PushContinuousRequest(continuousHandler);
                    }
                    else if (handler is ResonanceResponseHandler responseHandler)
                    {
                        PushResponse(responseHandler);
                    }
                }
            }
            catch (ThreadAbortException)
            {
                LogManager.Log($"{GetExtendedComponentName()}: Push thread has been aborted.");
            }
            catch (Exception ex)
            {
                OnFailed(ex);
            }
        }

        private void PushRequest(ResonanceRequestHandler requestHandler)
        {
            if (requestHandler.Config.ShouldLog)
            {
                LogManager.Log($"{GetExtendedComponentName()}: Sending request '{requestHandler.Request.Message.GetType()}'...\n{requestHandler.Request.Message.ToJsonString()}", LogLevel.Info);
            }

            _pendingRequests.Add(requestHandler);

            ResonanceTranscodingInformation info = new ResonanceTranscodingInformation();
            info.Token = requestHandler.Request.Token;
            info.Message = requestHandler.Request.Message;

            byte[] data = Encoder.Encode(info);
            Adapter.Write(data);

            Task.Delay(requestHandler.Config.Timeout.Value).ContinueWith((x) =>
            {
                if (!requestHandler.CompletionSource.Task.IsCompleted)
                {
                    _pendingRequests.Remove(requestHandler);
                    requestHandler.CompletionSource.SetException(new TimeoutException($"{requestHandler.Request.Message.GetType()} was not provided with a response within the given period of {requestHandler.Config.Timeout.Value.Seconds} seconds and has timed out."));
                }
            });

            OnRequestSent(requestHandler.Request);
        }

        private void PushContinuousRequest(ResonanceContinuousRequestHandler requestHandler)
        {
            if (requestHandler.Config.ShouldLog)
            {
                LogManager.Log($"{GetExtendedComponentName()}: Sending continuous request '{requestHandler.Request.Message.GetType()}'...\n{requestHandler.Request.Message.ToJsonString()}", LogLevel.Info);
            }

            _pendingRequests.Add(requestHandler);

            ResonanceTranscodingInformation info = new ResonanceTranscodingInformation();
            info.Token = requestHandler.Request.Token;
            info.Message = requestHandler.Request.Message;

            byte[] data = Encoder.Encode(info);
            Adapter.Write(data);

            Task.Delay(requestHandler.Config.Timeout.Value).ContinueWith((x) =>
            {
                if (!requestHandler.ContinuousObservable.FirstMessageArrived)
                {
                    _pendingRequests.Remove(requestHandler);
                    requestHandler.ContinuousObservable.OnError(new TimeoutException($"{requestHandler.Request.Message.GetType()} was not provided with a response within the given period of {requestHandler.Config.Timeout.Value.Seconds} seconds and has timed out."));
                }
                else
                {
                    if (requestHandler.Config.ContinuousTimeout != null)
                    {
                        Task.Factory.StartNew(async () =>
                        {
                            while (!requestHandler.ContinuousObservable.IsCompleted)
                            {
                                await Task.Delay(requestHandler.Config.ContinuousTimeout.Value).ContinueWith((y) =>
                                {
                                    if (!requestHandler.ContinuousObservable.IsCompleted)
                                    {
                                        if (DateTime.Now - requestHandler.ContinuousObservable.LastResponseTime > requestHandler.Config.ContinuousTimeout.Value)
                                        {
                                            TimeoutException ex = new TimeoutException($"{GetExtendedComponentName()}: Continuous request message '{requestHandler.Request.Message.GetType()}' had failed to provide a response for a period of {requestHandler.Config.ContinuousTimeout.Value.TotalSeconds} seconds and has timed out.");
                                            OnRequestFailed(requestHandler.Request, ex);
                                            requestHandler.ContinuousObservable.OnError(ex);
                                            return;
                                        }
                                    }
                                });
                            }
                        });
                    }
                }
            });

            OnRequestSent(requestHandler.Request);
        }

        private void PushResponse(ResonanceResponseHandler responseHandler)
        {
            if (responseHandler.Config.ShouldLog)
            {
                LogManager.Log($"{GetExtendedComponentName()}: Sending request '{responseHandler.Response.Message.GetType()}'...\n{responseHandler.Response.Message.ToJsonString()}", LogLevel.Info);
            }

            ResonanceTranscodingInformation info = new ResonanceTranscodingInformation();
            info.Token = responseHandler.Response.Token;
            info.Message = responseHandler.Response.Message;
            info.Completed = responseHandler.Config.Completed;
            info.ErrorMessage = responseHandler.Config.ErrorMessage;
            info.HasError = responseHandler.Config.HasError;

            byte[] data = Encoder.Encode(info);
            Adapter.Write(data);

            OnResponseSent(responseHandler.Response);
        }

        #endregion

        #region Pull

        private void PullThreadMethod()
        {
            throw new NotImplementedException();
        }

        #endregion

        protected virtual async void OnFailed(Exception ex)
        {
            if (State != ResonanceComponentState.Failed)
            {
                FailedStateException = ex;
                LogManager.Log(ex, $"{GetExtendedComponentName()}: Transporter failed.");
                State = ResonanceComponentState.Failed;

                await OnPostDisconnection();
            }
            else
            {
                LogManager.Log(ex, LogLevel.Warning, $"{GetExtendedComponentName()}: OnFailed called while state is already failed!");
            }
        }

        protected virtual async Task OnPostDisconnection()
        {
            //try
            //{
            //    if (_pullThread != null)
            //    {
            //        _pullThread.Abort();
            //        _pushThread.Abort();
            //        _keepAliveThread.Abort();
            //    }
            //}
            //catch { }

            //if (Adapter != null)
            //{
            //    await Adapter.Disconnect();
            //}

            //NotifyContinuousRequestMessagesDisconnection();
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
    }
}
