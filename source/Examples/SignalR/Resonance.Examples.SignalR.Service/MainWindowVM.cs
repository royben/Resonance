using Microsoft.Extensions.Logging;
using Resonance.Discovery;
using Resonance.Examples.Common;
using Resonance.Examples.Common.Messages;
using Resonance.Examples.SignalR.Common;
using Resonance.Servers.Tcp;
using Resonance.SignalR;
using Resonance.SignalR.Services;
using Resonance.Transcoding.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.Examples.SignalR.Service
{
    public class MainWindowVM : ResonanceViewModel
    {
        private ResonanceRegisteredService<DemoCredentials, DemoServiceInformation, DemoAdapterInformation> _service;

        private String _serviceId;
        /// <summary>
        /// Gets or sets the service id.
        /// </summary>
        public String ServiceId
        {
            get { return _serviceId; }
            set { _serviceId = value; RaisePropertyChangedAuto(); InvalidateRelayCommands(); }
        }

        /// <summary>
        /// Gets or sets the hub URL.
        /// </summary>
        public String HubUrl { get; set; }

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
        /// Gets or sets the connected clients.
        /// </summary>
        public ObservableCollection<ResonanceSignalRClient> Clients { get; set; }

        /// <summary>
        /// Gets or sets the start command.
        /// </summary>
        public RelayCommand StartCommand { get; set; }

        /// <summary>
        /// Gets or sets the stop command.
        /// </summary>
        public RelayCommand StopCommand { get; set; }

        /// <summary>
        /// Gets or sets the disconnect client command.
        /// </summary>
        public RelayCommand<ResonanceSignalRClient> DisconnectClientCommand { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindowVM"/> class.
        /// </summary>
        public MainWindowVM()
        {
            Clients = new ObservableCollection<ResonanceSignalRClient>();
            ServiceId = "Service 1";
            HubUrl = "http://localhost:8080/DemoHub";
            StartCommand = new RelayCommand(Start, () => !IsStarted && !String.IsNullOrWhiteSpace(ServiceId));
            StopCommand = new RelayCommand(Stop, () => IsStarted);
            DisconnectClientCommand = new RelayCommand<ResonanceSignalRClient>(DisconnectClient);
        }

        /// <summary>
        /// Starts the service.
        /// </summary>
        private async void Start()
        {
            if (!IsStarted)
            {
                //Start the TCP server.
                Logger.LogInformation("Registering service...");
                IsStarted = true;
                _service = await ResonanceServiceFactory.Default.RegisterServiceAsync<
                    DemoCredentials,
                    DemoServiceInformation,
                    DemoAdapterInformation>(
                    new DemoCredentials() { Name = ServiceId },
                    new DemoServiceInformation() { ServiceId = ServiceId },
                    HubUrl,
                    SignalRMode.Legacy);

                Logger.LogInformation("Service started.");

                _service.ConnectionRequest += _service_ConnectionRequest;
                _service.Reconnecting += _service_Reconnecting;
                _service.Error += _service_Error;
                _service.Reconnected += _service_Reconnected;
            }
        }

        private void _service_Reconnected(object sender, EventArgs e)
        {
            Logger.LogInformation("Service reconnected.");
        }

        private void _service_Error(object sender, ResonanceExceptionEventArgs e)
        {
            Logger.LogError("Service connection lost.", e.Exception);
        }

        private void _service_Reconnecting(object sender, EventArgs e)
        {
            Logger.LogWarning("Reconnecting service...");
        }

        /// <summary>
        /// Stops the service.
        /// </summary>
        private async void Stop()
        {
            if (IsStarted)
            {
                //Stop the tcp server.
                await _service.DisposeAsync();
                _service.ConnectionRequest -= _service_ConnectionRequest;

                IsStarted = false;

                //Disconnect all clients.
                Clients.ToList().ForEach(async x => await x.DisposeAsync());
                Clients.Clear();
            }
        }

        private async void _service_ConnectionRequest(object sender, ConnectionRequestEventArgs<DemoCredentials, DemoAdapterInformation> e)
        {
            if (!await ShowQuestionMessage($"Client wants to connect to this service. Do you accept?"))
            {
                e.Decline();
                return;
            }

            ResonanceSignalRClient newClient = new ResonanceSignalRClient();
            newClient.RemoteAdapterInformation = e.RemoteAdapterInformation;

            //Enable keep alive so we are aware of clients losing contact.
            newClient.KeepAliveConfiguration.Enabled = true;

            //Configure the transporter fail when the keep alive determines no connection.
            newClient.KeepAliveConfiguration.FailTransporterOnTimeout = true;

            newClient.StateChanged += OnClientStateChanged;

            var adapter = e.Accept();
            adapter.Credentials.Name = $"{ServiceId} {adapter}";

            newClient.CreateBuilder()
                .WithAdapter(adapter)
                .WithJsonTranscoding()
                .Build();

            newClient.RegisterRequestHandler<EchoTextRequest, EchoTextResponse>((request) =>
            {
                Logger.LogInformation($"{newClient.RemoteAdapterInformation.Name} says: {request.Message}");
                return new EchoTextResponse() { Message = "OK" };
            });

            await newClient.ConnectAsync();

            InvokeUI(() =>
            {
                Clients.Add(newClient);
            });

            Logger.LogInformation($"{newClient.RemoteAdapterInformation.Name} is now connected.");
        }

        private async void DisconnectClient(ResonanceSignalRClient client)
        {
            if (client != null)
            {
                await client.DisposeAsync();
                Clients.Remove(client);
            }
        }

        /// <summary>
        /// Handles a client's stage changed event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="ResonanceComponentStateChangedEventArgs"/> instance containing the event data.</param>
        private void OnClientStateChanged(object sender, ResonanceComponentStateChangedEventArgs e)
        {
            ResonanceSignalRClient client = sender as ResonanceSignalRClient;

            if (e.NewState == ResonanceComponentState.Failed)
            {
                Logger.LogWarning($"Client {client.RemoteAdapterInformation.Name} disconnected.");

                InvokeUI(() =>
                {
                    Clients.Remove(client);
                });
            }
        }
    }
}
