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

        /// <summary>
        /// Gets or sets the TCP server port.
        /// </summary>
        public int Port { get; set; }

        private String _serviceName;
        /// <summary>
        /// Gets or sets the service name.
        /// </summary>
        public String ServiceName
        {
            get { return _serviceName; }
            set { _serviceName = value; RaisePropertyChangedAuto(); InvalidateRelayCommands(); }
        }

        private bool _isStarted;
        /// <summary>
        /// Gets or sets a value indicating whether the service has started.
        /// </summary>
        public bool IsStarted
        {
            get { return _isStarted; }
            set { _isStarted = value; RaisePropertyChangedAuto(); InvalidateRelayCommands(); }
        }

        /// <summary>
        /// Gets or sets the start command.
        /// </summary>
        public RelayCommand StartCommand { get; set; }

        /// <summary>
        /// Gets or sets the stop command.
        /// </summary>
        public RelayCommand StopCommand { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindowVM"/> class.
        /// </summary>
        public MainWindowVM()
        {
            Port = 8888;
            ServiceName = "Service 1";
            StartCommand = new RelayCommand(Start, () => !IsStarted && !String.IsNullOrWhiteSpace(ServiceName));
            StopCommand = new RelayCommand(Stop, () => IsStarted);

            _discoveryService = new ResonanceUdpDiscoveryService<DiscoveryInfo, JsonEncoder>();

            _clients = new List<ResonanceTcpClient>();
        }

        /// <summary>
        /// Starts the service.
        /// </summary>
        private async void Start()
        {
            if (!IsStarted)
            {
                //Start the TCP server.
                IsStarted = true;
                _server = new ResonanceTcpServer(Port);
                _server.ConnectionRequest += OnConnectionRequest;
                await _server.StartAsync();

                //Start the discovery service.
                _discoveryService.DiscoveryInfo = new DiscoveryInfo() { ServiceName = ServiceName, Port = Port };
                await _discoveryService.StartAsync();
            }
        }

        /// <summary>
        /// Stops the service.
        /// </summary>
        private async void Stop()
        {
            if (IsStarted)
            {
                //Stop the tcp server.
                await _server.StopAsync();

                //Stop the discovery service.
                await _discoveryService.StopAsync();
                IsStarted = false;

                //Disconnect all clients.
                _clients.ToList().ForEach(async x => await x.DisconnectAsync());
                _clients.Clear();
            }
        }

        /// <summary>
        /// Handles incoming connections.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="ResonanceListeningServerConnectionRequestEventArgs{Adapters.Tcp.TcpAdapter}"/> instance containing the event data.</param>
        private async void OnConnectionRequest(object sender, ResonanceListeningServerConnectionRequestEventArgs<Adapters.Tcp.TcpAdapter> e)
        {
            ResonanceTcpClient newClient = new ResonanceTcpClient();

            //Enable keep alive so we are aware of clients losing contact.
            newClient.KeepAliveConfiguration.Enabled = true;

            //Configure the transporter fail when the keep alive determines no connection.
            newClient.KeepAliveConfiguration.FailTransporterOnTimeout = true;

            newClient.CreateBuilder()
                .WithAdapter(e.Accept())
                .WithJsonTranscoding()
                .Build();

            newClient.StateChanged += OnClientStateChanged;
            newClient.RegisterRequestHandler<LoginRequest, LoginResponse>(OnClientLoginRequest);
            newClient.RegisterRequestHandler<JoinSessionRequest, JoinSessionResponse>(OnClientJoinSessionRequest);
            newClient.RegisterRequestHandler<LeaveSessionRequest>(OnClientLeaveSessionRequest);

            await newClient.ConnectAsync();
        }

        /// <summary>
        /// Handles clients login request.
        /// </summary>
        /// <param name="transporter">The transporter.</param>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">Client {request.ClientID} is already taken.</exception>
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

        /// <summary>
        /// Handles clients "join session" request.
        /// </summary>
        /// <param name="transporter">The transporter.</param>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        /// <exception cref="AuthenticationException">Please login first.</exception>
        /// <exception cref="System.Exception">
        /// Cannot create a loop-back session!
        /// or
        /// Client {request.ClientID} was not found.
        /// or
        /// Client {request.ClientID} is already in session with another client.
        /// </exception>
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

            existingClient.SendRequestAsync<JoinSessionRequest, JoinSessionResponse>(new JoinSessionRequest()
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

        /// <summary>
        /// Handles clients "leave session" request.
        /// </summary>
        /// <param name="transporter">The transporter.</param>
        /// <param name="request">The request.</param>
        private async void OnClientLeaveSessionRequest(IResonanceTransporter transporter, ResonanceMessage<LeaveSessionRequest> request)
        {
            ResonanceTcpClient client = transporter as ResonanceTcpClient;

            if (client.InSession)
            {
                Logger.LogWarning($"Client {client.ClientID} has closed the session.");
                client.InSession = false;
                client.RemoteClient.InSession = false;

                await client.RemoteClient.SendObjectAsync(new LeaveSessionRequest()
                {
                    Reason = $"{client.RemoteClient.ClientID} has left the session"
                });

                client.RemoteClient.RemoteClient = null;
                client.RemoteClient = null;

                UpdateClientsList();
            }
        }

        /// <summary>
        /// Handles a client's stage changed event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="ResonanceComponentStateChangedEventArgs"/> instance containing the event data.</param>
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
                    await client.RemoteClient.SendObjectAsync(new LeaveSessionRequest()
                    {
                        Reason = "The remote client has disconnected"
                    });
                }
            }
        }

        /// <summary>
        /// Updates all clients with the available connected clients.
        /// </summary>
        private void UpdateClientsList()
        {
            _clients.ToList().ForEach(async x =>
            {
                await x.SendObjectAsync(new NotifyAvailableClientsRequest()
                {
                    Clients = _clients.Where(y => y != x).Select(y => y.ClientID).ToList()
                });
            });
        }
    }
}
