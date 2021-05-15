﻿using Microsoft.Extensions.Logging;
using Resonance.Discovery;
using Resonance.Examples.Common;
using Resonance.Examples.Common.Messages;
using Resonance.Examples.SignalR.Common;
using Resonance.Servers.Tcp;
using Resonance.SignalR;
using Resonance.SignalR.Discovery;
using Resonance.SignalR.Services;
using Resonance.Transcoding.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Resonance.Examples.SignalR.Client
{
    public class MainWindowVM : ResonanceViewModel
    {
        private IResonanceTransporter _transporter;
        private ResonanceSignalRDiscoveryClient<DemoServiceInformation, DemoCredentials> _discoveryClient;

        private bool _isConnected;
        /// <summary>
        /// Gets or sets a value indicating whether the client is connected to the remote server.
        /// </summary>
        public bool IsConnected
        {
            get { return _isConnected; }
            set { _isConnected = value; RaisePropertyChangedAuto(); InvalidateRelayCommands(); }
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
        public ObservableCollection<DemoServiceInformation> RegisteredServices { get; set; }

        private DemoServiceInformation selectedService;
        /// <summary>
        /// Gets or sets the selected service to connect to.
        /// </summary>
        public DemoServiceInformation SelectedService
        {
            get { return selectedService; }
            set { selectedService = value; RaisePropertyChangedAuto(); InvalidateRelayCommands(); }
        }

        /// <summary>
        /// Gets or sets the message to send.
        /// </summary>
        public String Message { get; set; }

        /// <summary>
        /// Gets or sets the connect command.
        /// </summary>
        public RelayCommand ConnectCommand { get; set; }

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
            HubUrl = "http://localhost:8080/DemoHub";

            ConnectCommand = new RelayCommand(Connect, () => !IsConnected && !String.IsNullOrWhiteSpace(ClientID) && SelectedService != null);
            DisconnectCommand = new RelayCommand(Disconnect, () => IsConnected);
            ResetDiscoveryCommand = new RelayCommand(ResetDiscovery, () => !IsConnected);

            SendMessageCommand = new RelayCommand(SendMessage, () => IsConnected);
            RegisteredServices = new ObservableCollection<DemoServiceInformation>();
        }

        protected override void OnApplicationReady()
        {
            base.OnApplicationReady();
            StartDiscovery();
        }

        private async void StartDiscovery()
        {
            _discoveryClient = new ResonanceSignalRDiscoveryClient<DemoServiceInformation, DemoCredentials>(
                HubUrl,
                SignalRMode.Legacy,
                new DemoCredentials() { Name = ClientID });

            _discoveryClient.ServiceDiscovered += OnServiceDiscovered;
            _discoveryClient.ServiceLost += OnServiceLost;

            await _discoveryClient.StartAsync();
        }

        private async void ResetDiscovery()
        {
            await _discoveryClient.StopAsync();
            RegisteredServices.Clear();
            _discoveryClient.HubUrl = HubUrl;
            _discoveryClient.Credentials = new DemoCredentials() { Name = ClientID };
            await _discoveryClient.StartAsync();
        }

        private void OnServiceLost(object sender, ResonanceDiscoveredServiceEventArgs<ResonanceSignalRDiscoveredService<DemoServiceInformation>, DemoServiceInformation> e)
        {
            InvokeUI(() =>
            {
                RegisteredServices.Remove(e.DiscoveredService.DiscoveryInfo);
            });
        }

        private void OnServiceDiscovered(object sender, ResonanceDiscoveredServiceEventArgs<ResonanceSignalRDiscoveredService<DemoServiceInformation>, DemoServiceInformation> e)
        {
            InvokeUI(() =>
            {
                RegisteredServices.Add(e.DiscoveredService.DiscoveryInfo);
            });
        }

        private async void Connect()
        {
            if (!IsConnected)
            {
                try
                {
                    IsFree = false;

                    _transporter = ResonanceTransporter.Builder
                        .Create()
                        .WithSignalRAdapter(SignalRMode.Legacy)
                        .WithCredentials(new DemoCredentials() { Name = ClientID })
                        .WithServiceId(SelectedService.ServiceId)
                        .WithUrl(HubUrl)
                        .WithJsonTranscoding()
                        .Build();

                    _transporter.ConnectionLost += ConnectionLost;

                    await _transporter.ConnectAsync();

                    await _discoveryClient.StopAsync();

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

        private async void Disconnect()
        {
            if (IsConnected)
            {
                await _transporter?.DisposeAsync();
                ClearConnection();
            }
        }

        private void ConnectionLost(object sender, ResonanceConnectionLostEventArgs e)
        {
            Logger.LogError("Connection lost: " + e.Exception.Message);
            ClearConnection();
        }

        private async void SendMessage()
        {
            var response = await _transporter.SendRequestAsync<EchoTextRequest, EchoTextResponse>(new EchoTextRequest()
            {
                Message = Message
            }, new ResonanceRequestConfig() { LoggingMode = ResonanceMessageLoggingMode.Content });
        }

        private void ClearConnection()
        {
            IsConnected = false;

            InvokeUI(async () =>
            {
                RegisteredServices.Clear();

                _discoveryClient.Credentials = new DemoCredentials() { Name = ClientID };
                _discoveryClient.HubUrl = HubUrl;
                await _discoveryClient.StartAsync();
            });
        }
    }
}
