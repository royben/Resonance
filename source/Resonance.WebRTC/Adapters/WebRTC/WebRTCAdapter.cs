using Microsoft.Extensions.Logging;
using Resonance.ExtensionMethods;
using Resonance.Threading;
using Resonance.WebRTC;
using Resonance.WebRTC.Exceptions;
using Resonance.WebRTC.Messages;
using SIPSorcery.Net;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Resonance.Adapters.WebRTC
{
    /// <summary>
    /// Represents a Resonance WebRTC adapter for communicating over WebRTC data channels.
    /// </summary>
    /// <seealso cref="Resonance.ResonanceAdapter" />
    public class WebRTCAdapter : ResonanceAdapter
    {
        private IResonanceTransporter _signalingTransporter;
        private RTCPeerConnection _connection;
        private List<RTCIceCandidate> _pendingCandidates;
        private RTCDataChannel _dataChannel;
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
        private bool _connectionInitialized;
        private bool _rolesReversed;

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

        /// <summary>
        /// Gets the name of the channel.
        /// This value is used to identity the adapter when multiple adapters are using the same signaling transporter, 
        /// and must match between the connecting and the accepting transporter.
        /// When using one adapter per signaling transporter there is no need to change this value.
        /// The default value is "resonance".
        /// </summary>
        public String ChannelName { get; internal set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Prevents a default instance of the <see cref="WebRTCAdapter"/> class from being created.
        /// </summary>
        private WebRTCAdapter()
        {
            _pendingCandidates = new List<RTCIceCandidate>();
            _incomingQueue = new ProducerConsumerQueue<byte[]>();
            IceServers = new List<WebRTCIceServer>();
            ChannelName = "resonance";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebRTCAdapter"/> class.
        /// </summary>
        /// <param name="signalingTransporter">The signaling transporter.</param>
        /// <param name="role">The adapter role.</param>
        public WebRTCAdapter(IResonanceTransporter signalingTransporter, WebRTCAdapterRole role) : this()
        {
            Role = role;
            _signalingTransporter = signalingTransporter;

            _signalingTransporter.RegisterRequestHandler<WebRTCIceCandidateRequest, WebRTCIceCandidateResponse>(OnWebRTCCandidateRequest);
            _signalingTransporter.RegisterRequestHandler<WebRTCOfferRequest, WebRTCOfferResponse>(OnWebRTCOfferRequest);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebRTCAdapter"/> class.
        /// </summary>
        /// <param name="signalingTransporter">The signaling transporter.</param>
        /// <param name="role">The role.</param>
        /// <param name="channelName">
        /// This value is used to identity the adapter when multiple adapters are using the same signaling transporter, 
        /// and must match between the connecting and the accepting transporter.
        /// When using one adapter per signaling transporter there is no need to change this value.
        /// The default value is "resonance".
        /// </param>
        public WebRTCAdapter(IResonanceTransporter signalingTransporter, WebRTCAdapterRole role, String channelName) : this(signalingTransporter, role)
        {
            ChannelName = channelName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebRTCAdapter"/> class.
        /// </summary>
        /// <param name="signalingTransporter">The signaling transporter.</param>
        /// <param name="offerRequest">The offer request.</param>
        /// <param name="requestToken">The offer request token.</param>
        public WebRTCAdapter(IResonanceTransporter signalingTransporter, WebRTCOfferRequest offerRequest, String requestToken) : this(signalingTransporter, WebRTCAdapterRole.Accept)
        {
            ChannelName = offerRequest.ChannelName;
            _offerRequest = offerRequest;
            _offerRequestToken = requestToken;
        }

        #endregion

        #region Connect / Disconnect / Write

        protected override Task OnConnect()
        {
            //SIPSorcery.LogFactory.Set(Resonance.ResonanceGlobalSettings.Default.LoggerFactory);

            _connectionInitialized = false;
            _rolesReversed = false;
            _connectionCompleted = false;
            _receivedSegments = new List<byte[]>();
            _expectedSegments = 0;
            _expectedSegmentsCheckSum = null;
            _incomingQueue = new ProducerConsumerQueue<byte[]>();

            _connectionCompletionSource = new TaskCompletionSource<object>();

            Logger.LogInformation("Initializing adapter with role '{Role}'.", Role);

            Task.Factory.StartNew(async () =>
            {
                try
                {
                    Thread.Sleep(50);

                    if (Role == WebRTCAdapterRole.Accept)
                    {
                        if (_offerRequest != null)
                        {
                            Logger.LogInformation("Adapter initialized by an offer request. Sending answer...");
                            var response = OnWebRTCOfferRequest(_offerRequest);
                            _signalingTransporter.SendResponse(response.Response, _offerRequestToken);
                        }
                        else
                        {
                            Logger.LogInformation("Waiting for offer...");
                        }
                    }
                    else
                    {
                        InitConnection();

                        Logger.LogInformation("Creating offer...");
                        RTCSessionDescriptionInit offer = _connection.createOffer(new RTCOfferOptions());

                        Logger.LogInformation("Setting local description...");
                        await _connection.setLocalDescription(offer);

                        Logger.LogInformation("Sending offer request...");
                        var response = await _signalingTransporter.SendRequestAsync<WebRTCOfferRequest, WebRTCOfferResponse>(new WebRTCOfferRequest()
                        {
                            ChannelName = ChannelName,
                            Offer = WebRTCSessionDescription.FromSessionDescription(offer)
                        }, new ResonanceRequestConfig()
                        {
                            Timeout = TimeSpan.FromSeconds(30)
                        });

                        if (response.Answer.InternalType == RTCSdpType.answer)
                        {
                            Logger.LogInformation("Answer received, setting remove description...");

                            var result = _connection.setRemoteDescription(response.Answer.ToSessionDescription());

                            if (result != SetDescriptionResultEnum.OK)
                            {
                                throw new Exception("Error setting the remote description.");
                            }
                        }
                        else
                        {
                            Logger.LogError($"Invalid answer type received '{response.Answer.InternalType}'.");
                        }

                        FlushIceCandidates();
                    }
                }
                catch (Exception ex)
                {
                    FailConnection(ex);
                }

            });

            return _connectionCompletionSource.Task;
        }

        protected override Task OnDisconnect()
        {
            return Task.Factory.StartNew(() =>
            {
                try
                {
                    State = ResonanceComponentState.Disconnected;
                    UnregisterRequestHandlers();
                    DisposeConnection();
                }
                catch { }
                finally
                {
                    _incomingQueue.BlockEnqueue(null);
                }
            });
        }

        protected override void OnWrite(byte[] data)
        {
            if (data.Length > MAX_MSG_SIZE)
            {
                var segments = data.ToChunks(MAX_MSG_SIZE);

                var firstSegment = segments[0];
                _dataChannel.send(BitConverter.GetBytes(segments.Count).Concat(MD5.Create().ComputeHash(data)).Concat(firstSegment).ToArray());
                segments.Remove(firstSegment);
                foreach (var segment in segments)
                {
                    _dataChannel.send(segment);
                }
            }
            else
            {
                _dataChannel.send(BitConverter.GetBytes(1).Concat(MD5.Create().ComputeHash(data)).Concat(data).ToArray());
            }
        }

        #endregion

        #region Init / Dispose

        private void InitConnection()
        {
            Logger.LogInformation("Initializing connection...");

            var servers = IceServers.DistinctBy(x => x.Url).ToList();

            foreach (var ice in servers)
            {
                Logger.LogInformation($"Adding ice server: '{ice}'...");
            }

            _connection = new RTCPeerConnection(new RTCConfiguration()
            {
                iceServers = servers.Select(x => new RTCIceServer()
                {
                    urls = x.Url,
                    username = x.UserName,
                    credential = x.Credentials,
                }).ToList()
            });

            _connection.ondatachannel += OnDataChannelInitialized;
            _connection.onicecandidate += OnIceCandidateAvailable;
            _connection.onconnectionstatechange += InConnectionStateChanged;

            Logger.LogInformation($"Creating data channel {ChannelName}...");
            var channel = _connection.createDataChannel(ChannelName).GetAwaiter().GetResult();

            _dataChannel = channel;
            _dataChannel.onopen += OnDataChannelOpened;
            _dataChannel.onclose += OnDataChannelClosed;

            Logger.LogInformation("Starting connection...");
            _connection.Start().GetAwaiter().GetResult();

            _connectionInitialized = true;
        }

        private void DisposeConnection()
        {
            try
            {
                Logger.LogInformation("Disposing connection...");

                _dataChannel?.close();

                if (_connection != null)
                {
                    _connection.ondatachannel += OnDataChannelInitialized;
                    _connection.onicecandidate += OnIceCandidateAvailable;
                    _connection.onconnectionstatechange += InConnectionStateChanged;
                    _connection.close();
                    _connection.Dispose();
                    _connection = null;
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Error occurred while trying to dispose the connection.");
            }
        }

        private void UnregisterRequestHandlers()
        {
            Logger.LogInformation("Unregistering request handlers...");

            _signalingTransporter.UnregisterRequestHandler<WebRTCIceCandidateRequest, WebRTCIceCandidateResponse>(OnWebRTCCandidateRequest);
            _signalingTransporter.UnregisterRequestHandler<WebRTCOfferRequest, WebRTCOfferResponse>(OnWebRTCOfferRequest);
        }

        /// <summary>
        /// Clears and fills the adapter Ice Servers list with the default, free, built-in servers.
        /// Use only for development/testing purpose, not production.
        /// </summary>
        public void AddDefaultIceServers()
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

            IceServers.AddRange(servers);
            IceServers = IceServers.DistinctBy(x => x.Url).ToList();
        }

        private void FailConnection(Exception ex, String message = null)
        {
            if (!_connectionCompleted)
            {
                if (message == null)
                {
                    Logger.LogError(ex, "Connection failed.");
                }
                else
                {
                    Logger.LogError(ex, $"Connection failed. {message}");
                }
                _connectionCompleted = true;
                _connectionCompletionSource.SetException(new ResonanceWebRTCConnectionFailedException(ex));
                UnregisterRequestHandlers();
                DisposeConnection();
            }
        }

        #endregion

        #region Signaling Request Handlers

        private ResonanceActionResult<WebRTCOfferResponse> OnWebRTCOfferRequest(WebRTCOfferRequest request)
        {
            if (request.ChannelName != ChannelName) return null;

            try
            {
                Logger.LogInformation("Offer received...");

                if (!_connectionInitialized)
                {
                    InitConnection();
                }

                Logger.LogInformation("Setting remote description...");
                var result = _connection.setRemoteDescription(request.Offer.ToSessionDescription());

                if (result != SetDescriptionResultEnum.OK)
                {
                    throw new Exception("Error setting remote description.");
                }

                if (request.Offer.InternalType == RTCSdpType.offer)
                {
                    Logger.LogInformation("Creating answer...");
                    var answer = _connection.createAnswer(null);

                    Logger.LogInformation("Setting local description...");
                    _connection.setLocalDescription(answer).GetAwaiter().GetResult();

                    Logger.LogInformation("Sending answer response...");
                    return new ResonanceActionResult<WebRTCOfferResponse>(
                        new WebRTCOfferResponse()
                        {
                            ChannelName = ChannelName,
                            Answer = WebRTCSessionDescription.FromSessionDescription(answer)
                        });
                }
                else
                {
                    Logger.LogError($"Invalid offer type received '{request.Offer.InternalType}'.");
                }

                throw new Exception("Invalid offer request.");
            }
            catch (Exception ex)
            {
                FailConnection(ex);
                throw ex;
            }
        }

        private ResonanceActionResult<WebRTCIceCandidateResponse> OnWebRTCCandidateRequest(WebRTCIceCandidateRequest request)
        {
            if (request.ChannelName != ChannelName) return null;

            try
            {
                Logger.LogInformation("Ice candidate request received. Adding...");

                _connection.addIceCandidate(new RTCIceCandidateInit()
                {
                    candidate = request.Candidate.Candidate,
                    sdpMid = request.Candidate.SdpMid,
                    sdpMLineIndex = request.Candidate.SdpMLineIndex,
                    usernameFragment = request.Candidate.UserNameFragment
                });

                FlushIceCandidates();

                return new ResonanceActionResult<WebRTCIceCandidateResponse>(new WebRTCIceCandidateResponse() { ChannelName = ChannelName });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error adding ice {@candidate}.", request.Candidate);
                throw new Exception("Error adding ice candidate.");
            }
        }

        #endregion

        #region WebRTC Event Handlers

        private void OnDataChannelInitialized(RTCDataChannel dataChannel)
        {
            Logger.LogInformation("Data channel initialized...");
            dataChannel.onmessage += OnDataChannelMessage;
        }

        private void OnDataChannelMessage(RTCDataChannel dc, DataChannelPayloadProtocols protocol, byte[] data)
        {
            if (protocol == DataChannelPayloadProtocols.WebRTC_Binary)
            {
                if (data != null)
                {
                    _incomingQueue.BlockEnqueue(data);
                }
            }
            else
            {
                Logger.LogWarning("None binary message received on the data channel.");
            }
        }

        private void OnDataChannelClosed()
        {
            if (State == ResonanceComponentState.Connected)
            {
                OnFailed(new ResonanceWebRTCChannelClosedException(), "The data channel has closed unexpectedly.");
            }
        }

        private void OnDataChannelOpened()
        {
            if (!_connectionCompleted)
            {
                Logger.LogInformation("Data channel opened.");

                _connectionCompleted = true;

                State = ResonanceComponentState.Connected;

                _receiveThread = new Thread(IncomingQueueThreadMethod);
                _receiveThread.IsBackground = true;
                _receiveThread.Start();

                _connectionCompletionSource.SetResult(true);
            }
        }

        private async void OnIceCandidateAvailable(RTCIceCandidate iceCandidate)
        {
            var candidate = new WebRTCIceCandidate()
            {
                Candidate = iceCandidate.candidate,
                SdpMid = iceCandidate.sdpMid,
                SdpMLineIndex = iceCandidate.sdpMLineIndex,
                UserNameFragment = iceCandidate.usernameFragment
            };

            if (_canSendIceCandidates)
            {
                Logger.LogInformation("New ice candidate found. Sending ice {@candidate} to remote peer.", candidate);

                try
                {
                    await _signalingTransporter.SendRequestAsync<WebRTCIceCandidateRequest, WebRTCIceCandidateResponse>(new WebRTCIceCandidateRequest()
                    {
                        ChannelName = ChannelName,
                        Candidate = candidate
                    }, new ResonanceRequestConfig() { Timeout = TimeSpan.FromSeconds(10) });
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error sending ice {@candidate} request.", candidate);
                }
            }
            else
            {
                Logger.LogInformation("New ice candidate found. queuing {@candidate}.", candidate);

                _pendingCandidates.Add(iceCandidate);
            }
        }

        private async void InConnectionStateChanged(RTCPeerConnectionState state)
        {
            if (state == RTCPeerConnectionState.failed)
            {
                if (!_connectionCompleted)
                {
                    if (!_rolesReversed)
                    {
                        Logger.LogInformation("First connection attempt failed. Reversing roles...");

                        _rolesReversed = true;
                        _canSendIceCandidates = false;

                        DisposeConnection();
                        InitConnection();

                        if (Role == WebRTCAdapterRole.Accept)
                        {
                            try
                            {
                                Logger.LogInformation("Creating offer...");

                                RTCSessionDescriptionInit offer = _connection.createOffer(new RTCOfferOptions());

                                Logger.LogInformation("Setting local description...");
                                await _connection.setLocalDescription(offer);

                                Logger.LogInformation("Sending offer request...");

                                var response = await _signalingTransporter.SendRequestAsync<WebRTCOfferRequest, WebRTCOfferResponse>(new WebRTCOfferRequest()
                                {
                                    ChannelName = ChannelName,
                                    Offer = WebRTCSessionDescription.FromSessionDescription(offer)
                                }, new ResonanceRequestConfig()
                                {
                                    Timeout = TimeSpan.FromSeconds(10)
                                });

                                if (response.Answer.InternalType == RTCSdpType.answer)
                                {
                                    var result = _connection.setRemoteDescription(response.Answer.ToSessionDescription());

                                    if (result != SetDescriptionResultEnum.OK)
                                    {
                                        throw new Exception("Error setting the remote description.");
                                    }
                                }
                                else
                                {
                                    Logger.LogError($"Invalid answer type received '{response.Answer.InternalType}'.");
                                }

                                FlushIceCandidates();
                            }
                            catch (Exception ex)
                            {
                                FailConnection(ex);
                            }
                        }
                    }
                    else
                    {
                        FailConnection(new Exception("Second connection attempt failed."));
                    }
                }
                else
                {
                    OnFailed(new ResonanceWebRTCChannelClosedException("The WebRTC connection has failed."));
                }
            }
        }

        #endregion

        #region Receive Queue

        private void IncomingQueueThreadMethod()
        {
            Logger.LogInformation("Message processing thread started...");

            while (State == ResonanceComponentState.Connected)
            {
                try
                {
                    byte[] data = _incomingQueue.BlockDequeue();
                    if (data == null)
                    {
                        Logger.LogInformation("Message processing thread terminated.");
                        return;
                    }

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
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error occurred while processing incoming data.");
                }
            }
        }

        #endregion

        #region Private Methods

        private async void FlushIceCandidates()
        {
            Logger.LogInformation("Flushing queued ice candidates...");

            _canSendIceCandidates = true;
            var pending = _pendingCandidates.ToList();
            _pendingCandidates.Clear();

            foreach (var iceCandidate in pending)
            {
                var candidate = new WebRTCIceCandidate()
                {
                    Candidate = iceCandidate.candidate,
                    SdpMid = iceCandidate.sdpMid,
                    SdpMLineIndex = iceCandidate.sdpMLineIndex,
                    UserNameFragment = iceCandidate.usernameFragment
                };

                try
                {
                    Logger.LogInformation("Sending ice {@candidate} to remote peer.", candidate);

                    await _signalingTransporter.SendRequestAsync<WebRTCIceCandidateRequest, WebRTCIceCandidateResponse>(new WebRTCIceCandidateRequest()
                    {
                        ChannelName = ChannelName,
                        Candidate = candidate
                    }, new ResonanceRequestConfig() { Timeout = TimeSpan.FromSeconds(10) });
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error sending ice {@candidate} request.", candidate);
                }
            }
        }

        #endregion
    }
}
