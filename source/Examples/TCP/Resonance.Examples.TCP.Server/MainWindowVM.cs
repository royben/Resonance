using Microsoft.Extensions.Logging;
using Resonance.Discovery;
using Resonance.Examples.Common;
using Resonance.Examples.Common.Messages;
using Resonance.Servers.Tcp;
using Resonance.Transcoding.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.Examples.TCP.Server
{
    public class MainWindowVM : ResonanceViewModel
    {
        private ResonanceTcpServer _server;
        private ResonanceUdpDiscoveryService<DiscoveryInfo, JsonEncoder> _discoveryService;
        private List<ResonanceTcpClient> _clients;

        public int Port { get; set; }

        private String _serviceName;
        public String ServiceName
        {
            get { return _serviceName; }
            set { _serviceName = value; RaisePropertyChangedAuto(); InvalidateRelayCommands(); }
        }

        private bool _isStarted;
        public bool IsStarted
        {
            get { return _isStarted; }
            set { _isStarted = value; RaisePropertyChangedAuto(); InvalidateRelayCommands(); }
        }

        public RelayCommand StartCommand { get; set; }

        public RelayCommand StopCommand { get; set; }

        public MainWindowVM()
        {
            Port = 8888;
            ServiceName = "Service 1";
            StartCommand = new RelayCommand(Start, () => !IsStarted && !String.IsNullOrWhiteSpace(ServiceName));
            StopCommand = new RelayCommand(Stop, () => IsStarted);

            _discoveryService = new ResonanceUdpDiscoveryService<DiscoveryInfo, JsonEncoder>();

            _clients = new List<ResonanceTcpClient>();
        }

        private async void Start()
        {
            if (!IsStarted)
            {
                IsStarted = true;
                _server = new ResonanceTcpServer(Port);
                _server.ConnectionRequest += _server_ConnectionRequest;
                await _server.Start();

                _discoveryService.DiscoveryInfo = new DiscoveryInfo() { ServiceName = ServiceName, Port = Port };
                await _discoveryService.Start();
            }
        }

        private async void Stop()
        {
            if (IsStarted)
            {
                await _server.Stop();
                await _discoveryService.Stop();
                IsStarted = false;

                _clients.ToList().ForEach(async x => await x.Disconnect());

                _clients.Clear();
            }
        }

        private async void _server_ConnectionRequest(object sender, ResonanceListeningServerConnectionRequestEventArgs<Adapters.Tcp.TcpAdapter> e)
        {
            ResonanceTcpClient newClient = new ResonanceTcpClient();
            newClient.KeepAliveConfiguration.Enabled = true;
            newClient.KeepAliveConfiguration.FailTransporterOnTimeout = true;

            newClient.CreateBuilder()
                .WithAdapter(e.Accept())
                .WithJsonTranscoding()
                .Build();

            newClient.StateChanged += OnClientStateChanged;
            newClient.RegisterRequestHandler<LoginRequest, LoginResponse>(OnClientLoginRequest);
            newClient.RegisterRequestHandler<JoinSessionRequest, JoinSessionResponse>(OnClientJoinSessionRequest);
            newClient.RegisterRequestHandler<LeaveSessionRequest>(OnClientLeaveSessionRequest);

            await newClient.Connect();
        }

        private ResonanceActionResult<JoinSessionResponse> OnClientJoinSessionRequest(IResonanceTransporter transporter, JoinSessionRequest request)
        {
            ResonanceTcpClient client = transporter as ResonanceTcpClient;

            if (client.ClientID == null)
            {
                throw new AuthenticationException("Please login first.");
            }

            var existingClient = _clients.FirstOrDefault(x => x.ClientID == request.ClientID);

            if (client == existingClient)
            {
                throw new Exception($"Cannot create a loop-back session!");
            }

            if (existingClient == null)
            {
                throw new Exception($"Client {request.ClientID} was not found.");
            }

            if (existingClient.InSession)
            {
                throw new Exception($"Client {request.ClientID} is already in session with another client.");
            }

            existingClient.SendRequest<JoinSessionRequest, JoinSessionResponse>(new JoinSessionRequest()
            {
                ClientID = client.ClientID
            },new ResonanceRequestConfig() { Timeout = TimeSpan.FromSeconds(10) }).GetAwaiter().GetResult();

            Task.Delay(100).ContinueWith((x) => //Delay the IsSession true to avoid routing of the JoinSessionResponse..
            {
                client.RemoteClient = existingClient;
                existingClient.RemoteClient = client;
                client.InSession = true;
                existingClient.InSession = true;

                Logger.LogInformation($"Clients {client.ClientID} and {existingClient.ClientID} are now in session.");
            });

            return new JoinSessionResponse();
        }

        private async void OnClientLeaveSessionRequest(IResonanceTransporter transporter, ResonanceRequest<LeaveSessionRequest> request)
        {
            ResonanceTcpClient client = transporter as ResonanceTcpClient;

            if (client.InSession)
            {
                Logger.LogWarning($"Client {client.ClientID} has closed the session.");
                client.InSession = false;
                client.RemoteClient.InSession = false;

                await client.RemoteClient.SendObject(new LeaveSessionRequest()
                {
                    Reason = $"{client.RemoteClient.ClientID} has left the session"
                });

                client.RemoteClient.RemoteClient = null;
                client.RemoteClient = null;

                UpdateClientsList();
            }
        }

        private ResonanceActionResult<LoginResponse> OnClientLoginRequest(IResonanceTransporter transporter, LoginRequest request)
        {
            ResonanceTcpClient client = transporter as ResonanceTcpClient;

            if (_clients.Exists(x => x.ClientID == request.ClientID))
            {
                throw new Exception($"Client {request.ClientID} is already taken.");
            }

            client.ClientID = request.ClientID;
            _clients.Add(client);

            Logger.LogInformation($"{client.ClientID} is now connected.");

            UpdateClientsList();

            return new LoginResponse();
        }

        private async void OnClientStateChanged(object sender, ResonanceComponentStateChangedEventArgs e)
        {
            ResonanceTcpClient client = sender as ResonanceTcpClient;

            if (e.NewState == ResonanceComponentState.Failed)
            {
                Logger.LogWarning($"Client {client.ClientID} disconnected.");

                _clients.Remove(client);

                UpdateClientsList();

                if (client.InSession)
                {
                    client.RemoteClient.InSession = false;
                    await client.RemoteClient.SendObject(new LeaveSessionRequest()
                    {
                        Reason = "The remote client has disconnected"
                    });
                }
            }
        }

        private void UpdateClientsList()
        {
            _clients.ToList().ForEach(async x =>
            {
                await x.SendObject(new NotifyAvailableClientsRequest()
                {
                    Clients = _clients.Where(y => y != x).Select(y => y.ClientID).ToList()
                });
            });
        }
    }
}
