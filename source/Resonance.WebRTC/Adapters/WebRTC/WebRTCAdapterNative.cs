using Microsoft.Extensions.Logging;
using Resonance.ExtensionMethods;
using Resonance.Threading;
using Resonance.WebRTC;
using Resonance.WebRTC.Exceptions;
using Resonance.WebRTC.Messages;
using SIPSorcery.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Resonance.Adapters.WebRTC
{
    public class WebRTCAdapterNative : ResonanceAdapter
    {
        private IResonanceTransporter _signalingTransporter;
        private ManagedConductor _conductor;
        private Thread _conductorThread;
        private List<WebRTCIceCandidate> _pendingCandidates;
        private TaskCompletionSource<object> _connectionCompletionSource;
        private bool _canSendIceCandidates;
        private WebRTCOfferRequest _offerRequest;
        private String _offerRequestToken;
        private const int MAX_MSG_SIZE = 16000; //~16 KB + segments count and checksum.
        private List<byte[]> _receivedSegments;
        private int _expectedSegments;
        private byte[] _expectedSegmentsCheckSum;
        private Thread _receiveThread;
        private ProducerConsumerQueue<byte[]> _incomingQueue;
        private bool _connectionCompleted;
        private bool _closeConnection;
        private bool _conductorInitialized;

        #region Properties

        /// <summary>
        /// The list of stun and turn servers.
        /// </summary>
        public List<WebRTCIceServer> IceServers { get; private set; }

        /// <summary>
        /// Gets this adapter role in the WebRTC session.
        /// </summary>
        public WebRTCAdapterRole Role { get; private set; }

        /// <summary>
        /// Gets or sets the connection timeout.
        /// Meaning, the data channel must be establish within this given time.
        /// </summary>
        public TimeSpan ConnectionTimeout { get; set; }

        /// <summary>
        /// Gets a value indicating whether this adapter was initialized by an offer request.
        /// </summary>
        public bool InitializedByOffer
        {
            get { return _offerRequest != null; }
        }

        /// <summary>
        /// Gets the signaling transporter of this adapter, used to exchange session description and ice candidates.
        /// </summary>
        public IResonanceTransporter SignalingTransporter
        {
            get { return _signalingTransporter; }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Prevents a default instance of the <see cref="WebRTCAdapter"/> class from being created.
        /// </summary>
        private WebRTCAdapterNative()
        {
            ConnectionTimeout = TimeSpan.FromSeconds(30);
            _pendingCandidates = new List<WebRTCIceCandidate>();
            _incomingQueue = new ProducerConsumerQueue<byte[]>();
            IceServers = new List<WebRTCIceServer>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebRTCAdapter"/> class.
        /// </summary>
        /// <param name="signalingTransporter">The signaling transporter.</param>
        /// <param name="role">The adapter role.</param>
        public WebRTCAdapterNative(IResonanceTransporter signalingTransporter, WebRTCAdapterRole role) : this()
        {
            Role = role;
            _signalingTransporter = signalingTransporter;

            if (_offerRequest == null)
            {
                _signalingTransporter.RegisterRequestHandler<WebRTCOfferRequest, WebRTCOfferResponse>(OnWebRTCOfferRequest);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebRTCAdapter"/> class.
        /// </summary>
        /// <param name="signalingTransporter">The signaling transporter.</param>
        /// <param name="offerRequest">The offer request.</param>
        /// <param name="requestToken">The offer request token.</param>
        public WebRTCAdapterNative(IResonanceTransporter signalingTransporter, WebRTCOfferRequest offerRequest, String requestToken) : this(signalingTransporter, WebRTCAdapterRole.Accept)
        {
            _offerRequest = offerRequest;
            _offerRequestToken = requestToken;
        }

        #endregion

        protected override Task OnConnect()
        {
            _connectionCompleted = false;
            _closeConnection = false;
            _receivedSegments = new List<byte[]>();
            _expectedSegments = 0;
            _expectedSegmentsCheckSum = null;
            _incomingQueue = new ProducerConsumerQueue<byte[]>();
            _conductorInitialized = false;

            _connectionCompletionSource = new TaskCompletionSource<object>();

            _signalingTransporter.RegisterRequestHandler<WebRTCIceCandidateRequest, WebRTCIceCandidateResponse>(OnWebRTCCandidateRequest);

            Logger.LogDebug("Initializing adapter with role '{Role}'.", Role);

            if (Role == WebRTCAdapterRole.Accept)
            {
                ThreadFactory.StartNew(async () =>
                {
                    try
                    {
                        await InitConnection();

                        if (_offerRequest != null)
                        {
                            Logger.LogDebug("Adapter initialized by an offer request. sending answer...");
                            var response = await OnWebRTCOfferRequest(_offerRequest);
                            if (_closeConnection) return;
                            _signalingTransporter.SendResponse(response.Response, _offerRequestToken);
                        }
                        else
                        {
                            Logger.LogDebug("Waiting for WebRTC offer...");
                        }
                    }
                    catch (Exception ex)
                    {
                        if (!_connectionCompleted)
                        {
                            _connectionCompleted = true;
                            _connectionCompletionSource.SetException(new ResonanceWebRTCConnectionFailedException(ex));
                            _closeConnection = true;
                        }
                    }
                });
            }
            else
            {
                ThreadFactory.StartNew(async () =>
                {
                    try
                    {
                        await InitConnection();

                        var offer = await CreateOffer();

                        var response = await _signalingTransporter.SendRequestAsync<WebRTCOfferRequest, WebRTCOfferResponse>(new WebRTCOfferRequest()
                        {
                            Offer = offer,
                        }, new ResonanceRequestConfig()
                        {
                            Timeout = TimeSpan.FromSeconds(30)
                        });

                        _conductor.OnOfferReply("answer", response.Answer.Sdp);

                        FlushIceCandidates();
                    }
                    catch (Exception ex)
                    {
                        if (!_connectionCompleted)
                        {
                            _connectionCompleted = true;
                            _connectionCompletionSource.SetException(new ResonanceWebRTCConnectionFailedException(ex));
                            _closeConnection = true;
                        }
                    }
                });
            }

            TimeoutTask.StartNew(() =>
            {

                if (!_connectionCompleted)
                {
                    _connectionCompleted = true;
                    _connectionCompletionSource.SetException(new ResonanceWebRTCConnectionFailedException(new TimeoutException("Could not initialize the connection within the given timeout.")));
                    _closeConnection = true;
                }

            }, ConnectionTimeout);

            return _connectionCompletionSource.Task;
        }

        protected override Task OnDisconnect()
        {
            State = ResonanceComponentState.Disconnected;
            _incomingQueue.BlockEnqueue(null);
            _closeConnection = true;
            return Task.FromResult(true);
        }

        protected override void OnWrite(byte[] data)
        {
            if (data.Length > MAX_MSG_SIZE)
            {
                var segments = data.ToChunks(MAX_MSG_SIZE);

                var firstSegment = segments[0];
                _conductor.DataChannelSendData(BitConverter.GetBytes(segments.Count).Concat(MD5.Create().ComputeHash(data)).Concat(firstSegment).ToArray());
                segments.Remove(firstSegment);
                foreach (var segment in segments)
                {
                    _conductor.DataChannelSendData(segment);
                }
            }
            else
            {
                _conductor.DataChannelSendData(BitConverter.GetBytes(1).Concat(MD5.Create().ComputeHash(data)).Concat(data).ToArray());
            }
        }

        #region Init

        private Task InitConnection()
        {
            TaskCompletionSource<bool> completion = new TaskCompletionSource<bool>();

            _conductorThread = new Thread(() =>
            {
                Thread.Sleep(5); //Wait for function to return at least!

                try
                {
                    _conductor = new ManagedConductor();
                    ManagedConductor.InitializeSSL();

                    foreach (var server in IceServers)
                    {
                        _conductor.AddServerConfig(server.Url, server.UserName, server.Credentials);
                    }

                    _conductor.AddServerConfig("stun:stun1.l.google.com:19302", String.Empty, String.Empty);
                    _conductor.AddServerConfig("stun:stun2.l.google.com:19302", String.Empty, String.Empty);

                    _conductor.SetAudio(false);
                    _conductor.SetVideoCapturer(640, 480, 5, false);

                    if (!_conductor.InitializePeerConnection())
                    {
                        completion.SetException(new ResonanceWebRTCConnectionFailedException(new Exception("Error initializing peer connection.")));
                        return;
                    }

                    _conductor.CreateDataChannel("resonance");
                    _conductor.OnIceCandidate += _conductor_OnIceCandidate;
                    _conductor.OnDataBinaryMessage += _conductor_OnDataBinaryMessage;
                    _conductor.OnError += _conductor_OnError;
                    _conductor.OnFailure += _conductor_OnFailure;
                    _conductor.OnIceStateChanged += _conductor_OnIceStateChanged;

                    _conductor.ProcessMessages(1000);
                }
                catch (Exception ex)
                {
                    completion.SetException(ex);
                    return;
                }

                _conductorInitialized = true;
                completion.SetResult(true);

                while (!_closeConnection)
                {
                    _conductor.ProcessMessages(1000);
                    Thread.Sleep(10);
                }

                _conductor.OnIceCandidate -= _conductor_OnIceCandidate;
                _conductor.OnDataBinaryMessage -= _conductor_OnDataBinaryMessage;
                _conductor.OnError -= _conductor_OnError;
                _conductor.OnFailure -= _conductor_OnFailure;
                _conductor.OnIceStateChanged -= _conductor_OnIceStateChanged;

                try
                {
                    _conductor.Dispose();
                }
                catch { }
            });

            _conductorThread.SetApartmentState(ApartmentState.STA);
            _conductorThread.IsBackground = true;
            _conductorThread.Start();

            return completion.Task;
        }

        /// <summary>
        /// Clears and fills the adapter Ice Servers list with the default, free, built-in servers.
        /// Use only for development/testing purpose, not production.
        /// </summary>
        public void InitDefaultIceServers()
        {
            var servers = new List<WebRTCIceServer>()
            {
                 new WebRTCIceServer() { Url = "stun:stun1.l.google.com:19302" },
                 new WebRTCIceServer() { Url = "stun:stun2.l.google.com:19302" },
                 new WebRTCIceServer() { Url = "stun:stun3.l.google.com:19302" },
                 new WebRTCIceServer() { Url = "stun:stun4.l.google.com:19302" },
                 new WebRTCIceServer() { Url = "stun:stun4.l.google.com:19302" },
                 new WebRTCIceServer() { Url = "stun:stun.sipsorcery.com" },
                 new WebRTCIceServer() { Url = "stun:eu-turn4.xirsys.com" },
                 new WebRTCIceServer() { Url = "turn:eu-turn4-back.xirsys.com:80?transport=udp", UserName = "DakLbB9dDKSF730T4aYcLeLIxXDfSNUIuXofS0-Geu-1vZN-MWYh6FaMDVy5-qWwAAAAAGCpr51TaXJpbGl4", Credentials = "1b037dd2-bb66-11eb-8a51-0242ac140004" },
                 new WebRTCIceServer() { Url = "turn:eu-turn4-back.xirsys.com:3478?transport=udp", UserName = "DakLbB9dDKSF730T4aYcLeLIxXDfSNUIuXofS0-Geu-1vZN-MWYh6FaMDVy5-qWwAAAAAGCpr51TaXJpbGl4", Credentials = "1b037dd2-bb66-11eb-8a51-0242ac140004" },
                 new WebRTCIceServer() { Url = "turn:eu-turn4-back.xirsys.com:80?transport=tcp", UserName = "DakLbB9dDKSF730T4aYcLeLIxXDfSNUIuXofS0-Geu-1vZN-MWYh6FaMDVy5-qWwAAAAAGCpr51TaXJpbGl4", Credentials = "1b037dd2-bb66-11eb-8a51-0242ac140004" },
                 new WebRTCIceServer() { Url = "turn:eu-turn4-back.xirsys.com:3478?transport=tcp", UserName = "DakLbB9dDKSF730T4aYcLeLIxXDfSNUIuXofS0-Geu-1vZN-MWYh6FaMDVy5-qWwAAAAAGCpr51TaXJpbGl4", Credentials = "1b037dd2-bb66-11eb-8a51-0242ac140004" }
            };

            IceServers.Clear();

            foreach (var server in servers)
            {
                IceServers.Add(server);
            }
        }

        #endregion

        #region Conductor Event Handlers

        private void _conductor_OnIceStateChanged(IceConnectionStates state)
        {
            if (!_closeConnection)
            {
                if (state == IceConnectionStates.kIceConnectionConnected)
                {
                    if (!_connectionCompleted)
                    {
                        _connectionCompleted = true;
                        _incomingQueue = new ProducerConsumerQueue<byte[]>();
                        _receiveThread = new Thread(IncomingQueueThreadMethod);
                        _receiveThread.IsBackground = true;
                        _receiveThread.Start();
                        _connectionCompletionSource.SetResult(true);
                    }
                }
                else if (state == IceConnectionStates.kIceConnectionFailed)
                {
                    if (_connectionCompleted)
                    {
                        OnFailed(new ResonanceWebRTCConnectionFailedException("Ice candidate connection failed."));
                    }
                    else
                    {
                        _connectionCompleted = true;
                        _connectionCompletionSource.SetException(new ResonanceWebRTCConnectionFailedException(new ResonanceWebRTCConnectionFailedException("Ice candidate connection failed.")));
                    }
                }
            }
        }

        private async void _conductor_OnIceCandidate(string sdp_mid, int sdp_mline_index, string sdp)
        {
            var candidate = new WebRTCIceCandidate()
            {
                SdpMid = sdp_mid,
                SdpMLineIndex = (ushort)sdp_mline_index,
                Candidate = sdp,
            };

            if (!_canSendIceCandidates)
            {
                _pendingCandidates.Add(candidate);
            }
            else
            {
                try
                {
                    await _signalingTransporter.SendRequestAsync<WebRTCIceCandidateRequest, WebRTCIceCandidateResponse>(new WebRTCIceCandidateRequest()
                    {
                        Candidate = candidate,
                    }, new ResonanceRequestConfig() { Timeout = TimeSpan.FromSeconds(10) });
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error sending ice candidate.");
                }
            }
        }

        private void _conductor_OnDataBinaryMessage(byte[] data)
        {
            if (data != null)
            {
                _incomingQueue.BlockEnqueue(data);
            }
        }

        private void _conductor_OnError()
        {
            if (_connectionCompleted)
            {
                OnFailed(new ResonanceWebRTCConnectionFailedException("Unspecified WebRTC error."));
            }
        }

        private void _conductor_OnFailure(string error)
        {
            if (_connectionCompleted)
            {
                OnFailed(new ResonanceWebRTCConnectionFailedException(error));
            }
        }

        #endregion

        #region Request Handlers

        private async Task<ResonanceActionResult<WebRTCOfferResponse>> OnWebRTCOfferRequest(WebRTCOfferRequest request)
        {
            try
            {
                while (!_conductorInitialized && !_closeConnection)
                {
                    Thread.Sleep(10);
                }

                if (_closeConnection) return null;

                var answer = await CreateAnswer(new WebRTCSessionDescription()
                {
                    Sdp = request.Offer.Sdp,
                    InternalType = RTCSdpType.offer
                });

                FlushIceCandidates();

                return new ResonanceActionResult<WebRTCOfferResponse>(new WebRTCOfferResponse() { Answer = answer });
            }
            catch (Exception ex)
            {
                if (!_connectionCompleted)
                {
                    _connectionCompleted = true;
                    _connectionCompletionSource.SetException(new ResonanceWebRTCConnectionFailedException(ex));
                }

                throw ex;
            }
        }

        private ResonanceActionResult<WebRTCIceCandidateResponse> OnWebRTCCandidateRequest(WebRTCIceCandidateRequest request)
        {
            try
            {
                ThreadFactory.StartNew(() =>
                {
                    _conductor.AddIceCandidate(request.Candidate.SdpMid, request.Candidate.SdpMLineIndex, request.Candidate.Candidate);
                });
                return new ResonanceActionResult<WebRTCIceCandidateResponse>(new WebRTCIceCandidateResponse());
            }
            catch (Exception ex)
            {
                throw new Exception("Error adding ice candidate", ex);
            }
        }

        #endregion

        #region Receive Queue

        private void IncomingQueueThreadMethod()
        {
            while (State == ResonanceComponentState.Connected)
            {
                byte[] data = _incomingQueue.BlockDequeue();
                if (data == null) return;

                Logger.LogInformation("DATA AVAILABLE");

                if (_expectedSegments == 0)
                {
                    _expectedSegments = BitConverter.ToInt32(data, 0);
                    _expectedSegmentsCheckSum = data.Skip(4).Take(16).ToArray();

                    byte[] segment = data.TakeFrom(20);

                    if (_expectedSegments == 1) //Take the shortcut if only one.
                    {
                        _expectedSegments = 0;

                        var checkSum = MD5.Create().ComputeHash(segment);
                        if (checkSum.SequenceEqual(_expectedSegmentsCheckSum))
                        {
                            OnDataAvailable(segment);
                        }
                        else
                        {
                            Logger.LogError("Message check sum failed. The message will be ignored.");
                        }
                    }
                    else
                    {
                        _receivedSegments.Add(segment);
                    }
                }
                else
                {
                    _receivedSegments.Add(data);

                    if (_receivedSegments.Count == _expectedSegments)
                    {
                        List<byte> allData = new List<byte>();

                        foreach (var segment in _receivedSegments)
                        {
                            allData.AddRange(segment);
                        }

                        _expectedSegments = 0;
                        _receivedSegments.Clear();

                        byte[] allDataBytes = allData.ToArray();

                        var checkSum = MD5.Create().ComputeHash(allDataBytes);

                        if (checkSum.SequenceEqual(_expectedSegmentsCheckSum))
                        {
                            OnDataAvailable(allDataBytes);
                        }
                        else
                        {
                            Logger.LogError("Message check sum failed. The message will be ignored.");
                        }
                    }
                }
            }
        }

        #endregion

        #region Private Methods

        private async void FlushIceCandidates()
        {
            _canSendIceCandidates = true;
            var pending = _pendingCandidates.ToList();
            _pendingCandidates.Clear();

            foreach (var iceCandidate in pending)
            {
                try
                {
                    await _signalingTransporter.SendRequestAsync<WebRTCIceCandidateRequest, WebRTCIceCandidateResponse>(new WebRTCIceCandidateRequest()
                    {
                        Candidate = iceCandidate,
                    }, new ResonanceRequestConfig() { Timeout = TimeSpan.FromSeconds(30) });
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error sending ice candidate.");
                }
            }
        }

        private Task<WebRTCSessionDescription> CreateOffer()
        {
            TaskCompletionSource<WebRTCSessionDescription> completion = new TaskCompletionSource<WebRTCSessionDescription>();

            ManagedConductor.OnCallbackSdp del = null;

            bool completed = false;

            del = (sdp) =>
            {
                if (!completed)
                {
                    completed = true;
                    _conductor.OnSuccessOffer -= del;
                    completion.SetResult(new WebRTCSessionDescription()
                    {
                        InternalType = RTCSdpType.offer,
                        Sdp = sdp
                    });
                }
            };

            _conductor.OnSuccessOffer += del;

            TimeoutTask.StartNew(() =>
            {
                if (!completed)
                {
                    completed = true;
                    completion.SetException(new TimeoutException("The offer was not created within the given time."));
                }
            }, TimeSpan.FromSeconds(10));

            _conductor.CreateOffer();

            return completion.Task;
        }

        private Task<WebRTCSessionDescription> CreateAnswer(WebRTCSessionDescription offer)
        {
            TaskCompletionSource<WebRTCSessionDescription> completion = new TaskCompletionSource<WebRTCSessionDescription>();

            ManagedConductor.OnCallbackSdp del = null;

            bool completed = false;

            del = (sdp) =>
            {
                if (!completed)
                {
                    completed = true;
                    _conductor.OnSuccessAnswer -= del;
                    completion.SetResult(new WebRTCSessionDescription() { Sdp = sdp, InternalType = RTCSdpType.answer });
                }
            };

            _conductor.OnSuccessAnswer += del;

            TimeoutTask.StartNew(() =>
            {
                if (!completed)
                {
                    completed = true;
                    completion.SetException(new TimeoutException("The answer was not created within the given time."));
                }
            }, TimeSpan.FromSeconds(10));

            _conductor.OnOfferRequest(offer.Sdp);

            return completion.Task;
        }

        #endregion
    }
}
