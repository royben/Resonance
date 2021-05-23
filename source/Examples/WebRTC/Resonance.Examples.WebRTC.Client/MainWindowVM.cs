using Microsoft.Extensions.Logging;
using Resonance.Adapters.WebRTC;
using Resonance.Discovery;
using Resonance.Examples.Common;
using Resonance.Examples.Common.Messages;
using Resonance.Examples.WebRTC.Common;
using Resonance.Servers.Tcp;
using Resonance.SignalR;
using Resonance.SignalR.Discovery;
using Resonance.SignalR.Services;
using Resonance.Transcoding.Json;
using Resonance.WebRTC.Messages;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Resonance.Examples.WebRTC.Client
{
    public class MainWindowVM : ResonanceViewModel
    {
        private IResonanceTransporter _signalingTransporter;
        private IResonanceTransporter _webRtcTransporter;
        private ResonanceSignalRDiscoveryClient<DemoServiceInformation, DemoCredentials> _discoveryClient;
        private ResonanceRegisteredService<DemoCredentials, DemoServiceInformation, DemoAdapterInformation> _service;

        #region Properties

        private bool _isConnected;
        /// <summary>
        /// Gets or sets a value indicating whether the listening service is registered and discovery has started.
        /// </summary>
        public bool IsConnected
        {
            get { return _isConnected; }
            set { _isConnected = value; RaisePropertyChangedAuto(); InvalidateRelayCommands(); }
        }

        private bool _isInSession;
        /// <summary>
        /// Gets or sets a value indicating whether a WebRTC session is currently active.
        /// </summary>
        public bool IsInSession
        {
            get { return _isInSession; }
            set { _isInSession = value; RaisePropertyChangedAuto(); InvalidateRelayCommands(); }
        }

        private String _clientID;
        /// <summary>
        /// Gets or sets the client unique identifier.
        /// </summary>
        public String ClientID
        {
            get { return _clientID; }
            set { _clientID = value; RaisePropertyChangedAuto(); InvalidateRelayCommands(); }
        }

        /// <summary>
        /// Gets or sets the hub URL.
        /// </summary>
        public String HubUrl { get; set; }

        /// <summary>
        /// Gets or sets the available services/clients discovered by the discovery client.
        /// </summary>
        public ObservableCollection<DemoServiceInformation> ConnectedClients { get; set; }

        private DemoServiceInformation selectedClient;
        /// <summary>
        /// Gets or sets the selected client for a session.
        /// </summary>
        public DemoServiceInformation SelectedClient
        {
            get { return selectedClient; }
            set { selectedClient = value; RaisePropertyChangedAuto(); InvalidateRelayCommands(); }
        }

        /// <summary>
        /// Gets or sets the current message to send.
        /// </summary>
        public String Message { get; set; }

        #endregion

        #region Commands

        public RelayCommand ConnectCommand { get; set; }

        public RelayCommand StartSessionCommand { get; set; }

        public RelayCommand LeaveSessionCommand { get; set; }

        public RelayCommand DisconnectCommand { get; set; }

        public RelayCommand SendMessageCommand { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindowVM"/> class.
        /// </summary>
        public MainWindowVM()
        {
            ClientID = "C" + new Random().Next(0, 100);
            HubUrl = "http://localhost:8081/DemoHub";

            ConnectCommand = new RelayCommand(Connect, () => !IsConnected && !String.IsNullOrWhiteSpace(ClientID));
            StartSessionCommand = new RelayCommand(StartSession, () => IsConnected && SelectedClient != null && !IsInSession);
            LeaveSessionCommand = new RelayCommand(async () => await LeaveSession(), () => IsInSession);

            DisconnectCommand = new RelayCommand(Disconnect, () => IsConnected);

            SendMessageCommand = new RelayCommand(SendMessage, () => IsConnected);
            ConnectedClients = new ObservableCollection<DemoServiceInformation>();
        }

        #endregion

        #region Connect / Disconnect

        /// <summary>
        /// Registers a listening service on the remote hub and starts a service discovery client
        /// to detect available services.
        /// </summary>
        private async void Connect()
        {
            if (!IsConnected)
            {
                try
                {
                    IsFree = false;

                    String serviceName = ClientID + " Service";


                    Logger.LogInformation("Initializing service and discovery...");
                    Logger.LogInformation($"Registering listening service {serviceName}...");

                    _service = await ResonanceServiceFactory.Default.RegisterServiceAsync<
                                DemoCredentials,
                                DemoServiceInformation,
                                DemoAdapterInformation>(
                                new DemoCredentials() { Name = serviceName },
                                new DemoServiceInformation() { ServiceId = ClientID },
                                HubUrl,
                                SignalRMode.Legacy);

                    _service.ConnectionRequest += OnServiceConnectionRequest;

                    Logger.LogInformation($"Starting service discovery...");

                    _discoveryClient = new ResonanceSignalRDiscoveryClient<DemoServiceInformation, DemoCredentials>(
                                HubUrl,
                                SignalRMode.Legacy,
                                new DemoCredentials() { Name = ClientID + " Discovery" });

                    _discoveryClient.ServiceDiscovered += OnServiceDiscovered;
                    _discoveryClient.ServiceLost += OnServiceLost;
                    _discoveryClient.Disconnected += OnDiscoveryError;

                    await _discoveryClient.StartAsync();

                    IsConnected = true;

                    Logger.LogInformation("Listening service and discovery started.");
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, ex.Message);
                }
                finally
                {
                    IsFree = true;
                }
            }
        }

        /// <summary>
        /// Stops the current WebRTC session, unregisters the listening service and stops the discovery
        /// </summary>
        private async void Disconnect()
        {
            if (IsConnected)
            {
                try
                {
                    Logger.LogInformation("Disconnecting...");

                    IsFree = false;

                    if (IsInSession)
                    {
                        await LeaveSession();
                    }

                    Logger.LogInformation("Closing listener service...");
                    await _service?.DisposeAsync();

                    Logger.LogInformation("Closing service discovery...");
                    await _discoveryClient?.DisposeAsync();

                    IsConnected = false;

                    InvokeUI(() =>
                    {
                        ConnectedClients.Clear();
                        SelectedClient = null;
                    });

                    Logger.LogInformation("Disconnected.");
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, ex.Message);
                }
                finally
                {
                    IsFree = true;
                }
            }
        }

        #endregion

        #region Service Discovery Event Handlers

        private void OnServiceLost(object sender, ResonanceDiscoveredServiceEventArgs<ResonanceSignalRDiscoveredService<DemoServiceInformation>, DemoServiceInformation> e)
        {
            InvokeUI(() =>
            {
                ConnectedClients.Remove(e.DiscoveredService.DiscoveryInfo);
            });
        }

        private void OnServiceDiscovered(object sender, ResonanceDiscoveredServiceEventArgs<ResonanceSignalRDiscoveredService<DemoServiceInformation>, DemoServiceInformation> e)
        {
            if (e.DiscoveredService.DiscoveryInfo.ServiceId != _service.ServiceInformation.ServiceId)
            {
                InvokeUI(() =>
                {
                    ConnectedClients.Add(e.DiscoveredService.DiscoveryInfo);

                    if (SelectedClient == null)
                    {
                        SelectedClient = e.DiscoveredService.DiscoveryInfo;
                    }
                });
            }
        }

        private void OnDiscoveryError(object sender, ResonanceExceptionEventArgs e)
        {
            Logger.LogError(e.Exception, $"Discovery stopped due to {e.Exception.Message}");
        }

        #endregion

        #region Incoming Connection Request Handler

        /// <summary>
        /// Handles incoming signaling connection request from the registered listening service.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="ConnectionRequestEventArgs{DemoCredentials, DemoAdapterInformation}"/> instance containing the event data.</param>
        private async void OnServiceConnectionRequest(object sender, ConnectionRequestEventArgs<DemoCredentials, DemoAdapterInformation> e)
        {
            Logger.LogInformation($"Connection request from {e.RemoteAdapterInformation.Name}.");

            if (IsInSession)
            {
                Logger.LogInformation($"Already in session. Connection request declined.");
                e.Decline();
                return;
            }

            try
            {
                Logger.LogInformation("Starting SignalR session for signaling...");

                SelectedClient = ConnectedClients.FirstOrDefault(x => x.ServiceId == e.RemoteAdapterInformation.Name);

                IsFree = false;

                _signalingTransporter = ResonanceTransporter.Builder
                    .Create()
                    .WithAdapter(e.Accept())
                    .WithJsonTranscoding()
                    .Build();

                _signalingTransporter.ConnectionLost += OnSignalingConnectionLost;

                _signalingTransporter.OnWebRtcOffer(async (request) =>
                {
                    try
                    {
                        Logger.LogInformation("WebRTC offer received...");
                        Logger.LogInformation("Connecting WebRTC transporter...");

                        _webRtcTransporter = ResonanceTransporter.Builder
                            .Create()
                            .WithWebRTCAdapter()
                            .WithSignalingTransporter(_signalingTransporter)
                            .WithOfferRequest(request)
                            .WithDefaultIceServers()
                            .WithJsonTranscoding()
                            .Build();

                        _webRtcTransporter.ConnectionLost += OnWebRTCConnectionLost;

                        _webRtcTransporter.RegisterRequestHandler<EchoTextRequest, EchoTextResponse>(OnEchoTextMessageReceived);

                        await _webRtcTransporter.ConnectAsync();

                        IsInSession = true;

                        Logger.LogInformation("WebRTC transporter connected!");
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, ex.Message);
                    }
                    finally
                    {
                        IsFree = true;
                    }
                });

                await _signalingTransporter.ConnectAsync();

                Logger.LogInformation("SignalR session started. Waiting for WebRTC offer...");
            }
            catch (Exception ex)
            {
                IsFree = true;
                Logger.LogError(ex, "");
            }
        }

        #endregion

        #region Start / Leave Session

        /// <summary>
        /// Starts a SignalR session and WebRTC session with the selected client.
        /// </summary>
        private async void StartSession()
        {
            if (!IsInSession)
            {
                try
                {
                    IsFree = false;

                    Logger.LogInformation($"Starting SignalR session with {SelectedClient.ServiceId} for signaling...");

                    _signalingTransporter = ResonanceTransporter.Builder
                        .Create()
                        .WithSignalRAdapter(SignalRMode.Legacy)
                        .WithCredentials(new DemoCredentials() { Name = ClientID })
                        .WithServiceId(SelectedClient.ServiceId)
                        .WithUrl(HubUrl)
                        .WithJsonTranscoding()
                        .Build();

                    _signalingTransporter.ConnectionLost += OnSignalingConnectionLost;

                    await _signalingTransporter.ConnectAsync();

                    Logger.LogInformation("SignalR session started.");
                    Logger.LogInformation("Initiating WebRTC session...");

                    _webRtcTransporter = ResonanceTransporter.Builder
                        .Create()
                        .WithWebRTCAdapter()
                        .WithSignalingTransporter(_signalingTransporter)
                        .WithRole(WebRTCAdapterRole.Connect)
                        .WithDefaultIceServers()
                        .WithJsonTranscoding()
                        .Build();

                    _webRtcTransporter.ConnectionLost += OnWebRTCConnectionLost;

                    _webRtcTransporter.RegisterRequestHandler<EchoTextRequest, EchoTextResponse>(OnEchoTextMessageReceived);

                    Logger.LogInformation("Connecting WebRTC transporter.");
                    await _webRtcTransporter.ConnectAsync();

                    IsInSession = true;

                    Logger.LogInformation("WebRTC transporter connected!");
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, ex.Message);
                }
                finally
                {
                    IsFree = true;
                }
            }
        }

        /// <summary>
        /// Leaves the current WebRTC session and Signaling session.
        /// </summary>
        private async Task LeaveSession()
        {
            if (IsInSession)
            {
                try
                {
                    IsFree = false;
                    Logger.LogInformation("Leaving session...");

                    Logger.LogInformation("Closing WebRTC session...");
                    await _webRtcTransporter.DisposeAsync();

                    Logger.LogInformation("Closing Signaling session...");
                    await _signalingTransporter.DisposeAsync();


                    Logger.LogInformation("Session closed!");
                    IsInSession = false;
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, ex.Message);
                }
                finally
                {
                    IsFree = true;
                }
            }
        }

        #endregion

        #region Connection Loss

        /// <summary>
        /// Handles signaling session connection loss.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="ResonanceConnectionLostEventArgs"/> instance containing the event data.</param>
        private void OnSignalingConnectionLost(object sender, ResonanceConnectionLostEventArgs e)
        {
            Logger.LogError("Signaling connection lost: " + e.Exception.Message);
        }

        /// <summary>
        /// Handles WebRTC session connection loss.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="ResonanceConnectionLostEventArgs"/> instance containing the event data.</param>
        private void OnWebRTCConnectionLost(object sender, ResonanceConnectionLostEventArgs e)
        {
            Logger.LogError("WebRTC connection lost: " + e.Exception.Message);
            IsInSession = false;
        }

        #endregion

        #region Echo Text Messaging

        /// <summary>
        /// Handles incoming echo text message on the WebRTC transporter.
        /// </summary>
        /// <param name="request">The request.</param>
        private ResonanceActionResult<EchoTextResponse> OnEchoTextMessageReceived(EchoTextRequest request)
        {
            Logger.LogInformation($"{SelectedClient.ServiceId} says: {request.Message}");
            return new EchoTextResponse() { Message = request.Message };
        }

        /// <summary>
        /// Sends an echo text message on the WebRTC transporter.
        /// </summary>
        private async void SendMessage()
        {
            if (IsInSession)
            {
                try
                {
                    Logger.LogInformation($"Sending message '{Message}' via WebRTC...");
                    var response = await _webRtcTransporter.SendRequestAsync<EchoTextRequest, EchoTextResponse>(new EchoTextRequest()
                    {
                        Message = Message
                    });
                    Logger.LogInformation($"Response echo received '{Message}'.");
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, ex.Message);
                }
            }
        }

        #endregion
    }
}
