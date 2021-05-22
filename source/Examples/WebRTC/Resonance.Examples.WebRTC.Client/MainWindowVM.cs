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
        private IResonanceTransporter _transporter;
        private IResonanceTransporter _webRTCTransporter;
        private ResonanceSignalRDiscoveryClient<DemoServiceInformation, DemoCredentials> _discoveryClient;
        private ResonanceRegisteredService<DemoCredentials, DemoServiceInformation, DemoAdapterInformation> _service;

        private bool _isConnected;
        /// <summary>
        /// Gets or sets a value indicating whether the client is connected to the remote server.
        /// </summary>
        public bool IsConnected
        {
            get { return _isConnected; }
            set { _isConnected = value; RaisePropertyChangedAuto(); InvalidateRelayCommands(); }
        }

        private bool _isInSession;
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
        /// Gets or sets the available services discovered by the discovery client.
        /// </summary>
        public ObservableCollection<DemoServiceInformation> ConnectedClients { get; set; }

        private DemoServiceInformation selectedClient;
        /// <summary>
        /// Gets or sets the selected service to connect to.
        /// </summary>
        public DemoServiceInformation SelectedClient
        {
            get { return selectedClient; }
            set { selectedClient = value; RaisePropertyChangedAuto(); InvalidateRelayCommands(); }
        }

        /// <summary>
        /// Gets or sets the message to send.
        /// </summary>
        public String Message { get; set; }

        /// <summary>
        /// Gets or sets the connect command.
        /// </summary>
        public RelayCommand ConnectCommand { get; set; }

        public RelayCommand StartSessionCommand { get; set; }

        public RelayCommand LeaveSessionCommand { get; set; }

        /// <summary>
        /// Gets or sets the disconnect command.
        /// </summary>
        public RelayCommand DisconnectCommand { get; set; }

        /// <summary>
        /// Gets or sets the send message command.
        /// </summary>
        public RelayCommand SendMessageCommand { get; set; }

        /// <summary>
        /// Gets or sets the reset discovery command.
        /// </summary>
        public RelayCommand ResetDiscoveryCommand { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindowVM"/> class.
        /// </summary>
        public MainWindowVM()
        {
            ClientID = "C" + new Random().Next(0, 100);
            HubUrl = "http://localhost:8081/DemoHub";

            ConnectCommand = new RelayCommand(Connect, () => !IsConnected && !String.IsNullOrWhiteSpace(ClientID));
            StartSessionCommand = new RelayCommand(StartSession, () => IsConnected && SelectedClient != null && !IsInSession);

            DisconnectCommand = new RelayCommand(Disconnect, () => IsConnected);
            ResetDiscoveryCommand = new RelayCommand(ResetDiscovery, () => !IsConnected && IsFree);

            SendMessageCommand = new RelayCommand(SendMessage, () => IsConnected);
            ConnectedClients = new ObservableCollection<DemoServiceInformation>();
        }

        protected override void OnApplicationReady()
        {
            base.OnApplicationReady();
        }

        private async void ResetDiscovery()
        {
            await _discoveryClient.StopAsync();
            ConnectedClients.Clear();
            _discoveryClient.HubUrl = HubUrl;
            _discoveryClient.Credentials = new DemoCredentials() { Name = ClientID };

            try
            {
                IsFree = false;
                await _discoveryClient.StartAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error starting discovery service. Make sure the remote WebRTC Hub is running.");
            }
            finally
            {
                IsFree = true;
            }
        }

        private async void Connect()
        {
            if (!IsConnected)
            {
                try
                {
                    IsFree = false;

                    _service = await ResonanceServiceFactory.Default.RegisterServiceAsync<
                                DemoCredentials,
                                DemoServiceInformation,
                                DemoAdapterInformation>(
                                new DemoCredentials() { Name = ClientID },
                                new DemoServiceInformation() { ServiceId = ClientID },
                                HubUrl,
                                SignalRMode.Legacy);

                    _service.ConnectionRequest += _service_ConnectionRequest;

                    _discoveryClient = new ResonanceSignalRDiscoveryClient<DemoServiceInformation, DemoCredentials>(
                                HubUrl,
                                SignalRMode.Legacy,
                                new DemoCredentials() { Name = ClientID });

                    _discoveryClient.ServiceDiscovered += OnServiceDiscovered;
                    _discoveryClient.ServiceLost += OnServiceLost;
                    _discoveryClient.Disconnected += OnDiscoveryError;

                    await _discoveryClient.StartAsync();

                    IsConnected = true;
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
                });
            }
        }

        private void OnDiscoveryError(object sender, ResonanceExceptionEventArgs e)
        {
            Logger.LogError(e.Exception, $"Discovery stopped due to {e.Exception.Message}");
        }

        private async void StartSession()
        {
            IsFree = false;

            _transporter = ResonanceTransporter.Builder
                .Create()
                .WithSignalRAdapter(SignalRMode.Legacy)
                .WithCredentials(new DemoCredentials() { Name = ClientID })
                .WithServiceId(SelectedClient.ServiceId)
                .WithUrl(HubUrl)
                .WithJsonTranscoding()
                .Build();

            await _transporter.ConnectAsync();

            _webRTCTransporter = ResonanceTransporter.Builder
                .Create()
                .WithWebRTCAdapter()
                .WithSignalingTransporter(_transporter)
                .WithRole(WebRTCAdapterRole.Connect)
                .WithDefaultIceServers()
                .WithJsonTranscoding()
                .Build();

            _webRTCTransporter.RegisterRequestHandler<EchoTextRequest, EchoTextResponse>(OnEchoTextMessageReceived);

            await _webRTCTransporter.ConnectAsync();

            IsInSession = true;
            IsFree = true;

            Logger.LogInformation("You are now connected through WebRTC !");
        }

        private async void _service_ConnectionRequest(object sender, ConnectionRequestEventArgs<DemoCredentials, DemoAdapterInformation> e)
        {
            if (IsInSession)
            {
                e.Decline();
                return;
            }

            SelectedClient = ConnectedClients.FirstOrDefault(x => x.ServiceId == e.RemoteAdapterInformation.Name);

            IsFree = false;

            _transporter = ResonanceTransporter.Builder
                .Create()
                .WithAdapter(e.Accept())
                .WithJsonTranscoding()
                .Build();

            _transporter.RegisterRequestHandler<WebRTCOfferRequest>(async (t, request) =>
            {
                _webRTCTransporter = ResonanceTransporter.Builder
                .Create()
                .WithWebRTCAdapter()
                .WithSignalingTransporter(_transporter)
                .WithOfferRequest(request.Object, request.Token)
                .WithDefaultIceServers()
                .WithJsonTranscoding()
                .Build();

                _webRTCTransporter.RegisterRequestHandler<EchoTextRequest, EchoTextResponse>(OnEchoTextMessageReceived);

                await _webRTCTransporter.ConnectAsync();

                IsInSession = true;

                Logger.LogInformation("You are now connected through WebRTC !");

                IsFree = true;
            });

            await _transporter.ConnectAsync();
        }

        private ResonanceActionResult<EchoTextResponse> OnEchoTextMessageReceived(EchoTextRequest request)
        {
            Logger.LogInformation($"{SelectedClient.ServiceId} says: {request.Message}");
            return new EchoTextResponse() { Message = request.Message };
        }

        private async void Disconnect()
        {
            if (IsConnected)
            {
                await _service.DisposeAsync();
            }
        }

        private void ConnectionLost(object sender, ResonanceConnectionLostEventArgs e)
        {
            Logger.LogError("Connection lost: " + e.Exception.Message);
            ClearConnection();
        }

        private async void SendMessage()
        {
            var response = await _webRTCTransporter.SendRequestAsync<EchoTextRequest, EchoTextResponse>(new EchoTextRequest()
            {
                Message = Message
            }, new ResonanceRequestConfig());
        }

        private void ClearConnection()
        {
            IsConnected = false;

            InvokeUI(async () =>
            {
                ConnectedClients.Clear();

                _discoveryClient.Credentials = new DemoCredentials() { Name = ClientID };
                _discoveryClient.HubUrl = HubUrl;
                await _discoveryClient.StartAsync();
            });
        }
    }
}
