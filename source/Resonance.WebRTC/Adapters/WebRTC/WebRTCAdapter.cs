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
        private WebRTCAdapter()
        {
            ConnectionTimeout = TimeSpan.FromSeconds(30);
            _pendingCandidates = new List<RTCIceCandidate>();
            _incomingQueue = new ProducerConsumerQueue<byte[]>();
            IceServers = new List<WebRTCIceServer>();
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
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebRTCAdapter"/> class.
        /// </summary>
        /// <param name="signalingTransporter">The signaling transporter.</param>
        /// <param name="offerRequest">The offer request.</param>
        /// <param name="requestToken">The offer request token.</param>
        public WebRTCAdapter(IResonanceTransporter signalingTransporter, WebRTCOfferRequest offerRequest, String requestToken) : this(signalingTransporter, WebRTCAdapterRole.Accept)
        {
            _offerRequest = offerRequest;
            _offerRequestToken = requestToken;
        }

        #endregion

        #region Connect / Disconnect / Write

        protected override Task OnConnect()
        {
            //SIPSorcery.LogFactory.Set(Resonance.ResonanceGlobalSettings.Default.LoggerFactory);

            _connectionCompleted = false;
            _receivedSegments = new List<byte[]>();
            _expectedSegments = 0;
            _expectedSegmentsCheckSum = null;
            _incomingQueue = new ProducerConsumerQueue<byte[]>();

            _connectionCompletionSource = new TaskCompletionSource<object>();

            _signalingTransporter.RegisterRequestHandler<WebRTCIceCandidateRequest, WebRTCIceCandidateResponse>(OnWebRTCCandidateRequest);

            Logger.LogDebug("Initializing adapter with role '{Role}'.", Role);

            if (Role == WebRTCAdapterRole.Accept)
            {
                if (_offerRequest == null)
                {
                    _signalingTransporter.RegisterRequestHandler<WebRTCOfferRequest, WebRTCOfferResponse>(OnWebRTCOfferRequest);
                }
                else
                {
                    Task.Factory.StartNew(() =>
                    {
                        try
                        {
                            Logger.LogDebug("Adapter initialized by an offer request.");
                            var response = OnWebRTCOfferRequest(_offerRequest);

                            _signalingTransporter.SendResponseAsync(response.Response, _offerRequestToken).GetAwaiter().GetResult();
                        }
                        catch (Exception ex)
                        {
                            if (!_connectionCompleted)
                            {
                                _connectionCompleted = true;
                                _connectionCompletionSource.SetException(new ResonanceWebRTCConnectionFailedException(ex));
                                CloseConnection();
                            }
                        }
                    });
                }
            }
            else
            {
                Task.Factory.StartNew(async () =>
                {
                    try
                    {
                        InitConnection();

                        RTCSessionDescriptionInit offer = _connection.createOffer(new RTCOfferOptions());
                        await _connection.setLocalDescription(offer);

                        var response = await _signalingTransporter.SendRequestAsync<WebRTCOfferRequest, WebRTCOfferResponse>(new WebRTCOfferRequest()
                        {
                            Offer = WebRTCSessionDescription.FromSessionDescription(offer)
                        }, new ResonanceRequestConfig()
                        {
                            Timeout = TimeSpan.FromSeconds(30)
                        });

                        if (response.Answer.InternalType == RTCSdpType.answer)
                        {
                            var result = _connection.setRemoteDescription(response.Answer.ToSessionDescription());

                            if (result != SetDescriptionResultEnum.OK)
                            {
                                throw new Exception("Error setting the remote description.");
                            }
                        }

                        _canSendIceCandidates = true;
                        await FlushIceCandidates();
                    }
                    catch (Exception ex)
                    {
                        if (!_connectionCompleted)
                        {
                            _connectionCompleted = true;
                            _connectionCompletionSource.SetException(new ResonanceWebRTCConnectionFailedException(ex));
                            CloseConnection();
                        }
                    }
                });
            }

            return _connectionCompletionSource.Task;
        }

        protected override Task OnDisconnect()
        {
            return Task.Factory.StartNew(() =>
            {
                try
                {
                    State = ResonanceComponentState.Disconnected;
                    CloseConnection();
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

        #region Init

        private void InitConnection()
        {
            _connection = new RTCPeerConnection(new RTCConfiguration()
            {
                iceServers = IceServers.Select(x => new RTCIceServer()
                {
                    urls = x.Url,
                    username = x.UserName,
                    credential = x.Credentials,
                }).ToList()
            });

            _connection.ondatachannel += OnDataChannelInitialized;
            _connection.onicecandidate += OnIceCandidateAvailable;

            var channel = _connection.createDataChannel("resonance").GetAwaiter().GetResult();

            _dataChannel = channel;
            _dataChannel.onopen += OnDataChannelOpened;
            _dataChannel.onclose += OnDataChannelClosed;

            _connection.Start().GetAwaiter().GetResult();

            TimeoutTask.StartNew(() =>
            {

                if (!_connectionCompleted)
                {
                    _connectionCompleted = true;
                    _connectionCompletionSource.SetException(new ResonanceWebRTCConnectionFailedException(new TimeoutException("Could not initialize the connection within the given timeout.")));
                    CloseConnection();
                }

            }, ConnectionTimeout);
        }

        private void CloseConnection()
        {
            try
            {
                _dataChannel?.close();
                _connection?.close();
                _connection?.Dispose();
            }
            catch { }
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

        #region Request Handlers

        private ResonanceActionResult<WebRTCOfferResponse> OnWebRTCOfferRequest(WebRTCOfferRequest request)
        {
            try
            {
                InitConnection();

                var result = _connection.setRemoteDescription(request.Offer.ToSessionDescription());

                if (result != SetDescriptionResultEnum.OK)
                {
                    throw new Exception("Error setting remote description.");
                }

                if (request.Offer.InternalType == RTCSdpType.offer)
                {
                    var answer = _connection.createAnswer(null);
                    _connection.setLocalDescription(answer).GetAwaiter().GetResult();

                    return new ResonanceActionResult<WebRTCOfferResponse>(
                        new WebRTCOfferResponse() { Answer = WebRTCSessionDescription.FromSessionDescription(answer) });
                }

                throw new Exception("Invalid offer request.");
            }
            catch (Exception ex)
            {
                if (!_connectionCompleted)
                {
                    _connectionCompleted = true;
                    _connectionCompletionSource.SetException(new ResonanceWebRTCConnectionFailedException(ex));
                    CloseConnection();
                }

                throw ex;
            }
        }

        private ResonanceActionResult<WebRTCIceCandidateResponse> OnWebRTCCandidateRequest(WebRTCIceCandidateRequest request)
        {
            try
            {
                _connection.addIceCandidate(new RTCIceCandidateInit()
                {
                    candidate = request.Candidate,
                    sdpMid = request.SdpMid,
                    sdpMLineIndex = request.SdpMLineIndex,
                    usernameFragment = request.UserNameFragment
                });

                Task.Factory.StartNew(async () =>
                {
                    _canSendIceCandidates = true;
                    await FlushIceCandidates();
                });

                return new ResonanceActionResult<WebRTCIceCandidateResponse>(new WebRTCIceCandidateResponse());
            }
            catch (Exception ex)
            {
                throw new Exception("Error adding ice candidate", ex);
            }
        }

        #endregion

        #region WebRTC Event Handlers

        private void OnDataChannelInitialized(RTCDataChannel dataChannel)
        {
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
        }

        private void OnDataChannelClosed()
        {
            if (State == ResonanceComponentState.Connected)
            {
                OnFailed(new ResonanceWebRTCChannelClosedException(), "The WebRTC data channel has closed unexpectedly.");
            }
        }

        protected virtual void OnDataChannelOpened()
        {
            if (!_connectionCompleted)
            {
                _connectionCompleted = true;

                State = ResonanceComponentState.Connected;

                _receiveThread = new Thread(IncomingQueueThreadMethod);
                _receiveThread.IsBackground = true;
                _receiveThread.Start();

                _connectionCompletionSource.SetResult(true);
            }
        }

        private void OnIceCandidateAvailable(RTCIceCandidate iceCandidate)
        {
            _pendingCandidates.Add(iceCandidate);
        }

        #endregion

        #region Receive Queue

        private void IncomingQueueThreadMethod()
        {
            while (State == ResonanceComponentState.Connected)
            {
                byte[] data = _incomingQueue.BlockDequeue();
                if (data == null) return;

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

        private async Task FlushIceCandidates()
        {
            if (_canSendIceCandidates)
            {
                var pending = _pendingCandidates.ToList();
                _pendingCandidates.Clear();

                foreach (var iceCandidate in pending)
                {
                    try
                    {
                        await _signalingTransporter.SendRequestAsync<WebRTCIceCandidateRequest, WebRTCIceCandidateResponse>(new WebRTCIceCandidateRequest()
                        {
                            Candidate = iceCandidate.candidate,
                            SdpMid = iceCandidate.sdpMid,
                            SdpMLineIndex = iceCandidate.sdpMLineIndex,
                            UserNameFragment = iceCandidate.usernameFragment
                        }, new ResonanceRequestConfig() { Timeout = TimeSpan.FromSeconds(5) });
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "Error sending ice candidate.");
                    }
                }
            }
        }

        #endregion
    }
}
