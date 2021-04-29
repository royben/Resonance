﻿using Microsoft.Extensions.Logging;
using Resonance.Discovery;
using Resonance.Examples.Common;
using Resonance.Examples.Common.Messages;
using Resonance.Servers.Tcp;
using Resonance.Transcoding.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.Examples.TCP.Client
{
    public class MainWindowVM : ResonanceViewModel
    {
        private IResonanceTransporter _transporter;
        private ResonanceUdpDiscoveryClient<DiscoveryInfo, JsonDecoder> _discoveryClient;

        private bool _isConnected;
        public bool IsConnected
        {
            get { return _isConnected; }
            set { _isConnected = value; RaisePropertyChangedAuto(); InvalidateRelayCommands(); }
        }

        private String _clientID;
        public String ClientID
        {
            get { return _clientID; }
            set { _clientID = value; RaisePropertyChangedAuto(); InvalidateRelayCommands(); }
        }

        public ObservableCollection<ResonanceUdpDiscoveredService<DiscoveryInfo>> DiscoveredServices { get; set; }

        private ResonanceUdpDiscoveredService<DiscoveryInfo> selectedService;
        public ResonanceUdpDiscoveredService<DiscoveryInfo> SelectedService
        {
            get { return selectedService; }
            set { selectedService = value; RaisePropertyChangedAuto(); InvalidateRelayCommands(); }
        }

        public ObservableCollection<String> ConnectedClients { get; set; }

        private String _selectedClient;
        public String SelectedClient
        {
            get { return _selectedClient; }
            set { _selectedClient = value; RaisePropertyChangedAuto(); InvalidateRelayCommands(); }
        }

        private bool _inSession;
        public bool InSession
        {
            get { return _inSession; }
            set { _inSession = value; RaisePropertyChangedAuto(); InvalidateRelayCommands(); }
        }

        public String Message { get; set; }

        public RelayCommand ConnectCommand { get; set; }

        public RelayCommand DisconnectCommand { get; set; }

        public RelayCommand JoinSessionCommand { get; set; }

        public RelayCommand LeaveSessionCommand { get; set; }

        public RelayCommand SendMessageCommand { get; set; }

        public MainWindowVM()
        {
            ConnectCommand = new RelayCommand(Connect, () => !IsConnected && !String.IsNullOrWhiteSpace(ClientID) && SelectedService != null);
            DisconnectCommand = new RelayCommand(Disconnect, () => IsConnected);
            JoinSessionCommand = new RelayCommand(JoinSession, () => IsConnected && SelectedClient != null && !InSession);
            LeaveSessionCommand = new RelayCommand(LeaveSession, () => InSession);

            SendMessageCommand = new RelayCommand(SendMessage, () => InSession);
            DiscoveredServices = new ObservableCollection<ResonanceUdpDiscoveredService<DiscoveryInfo>>();
            _discoveryClient = new ResonanceUdpDiscoveryClient<DiscoveryInfo, JsonDecoder>((info1, info2) => info1.Address == info2.Address && info1.DiscoveryInfo.ServiceName == info2.DiscoveryInfo.ServiceName);
            _discoveryClient.ServiceDiscovered += ServiceDiscovered;
            _discoveryClient.ServiceLost += ServiceLost;
            ConnectedClients = new ObservableCollection<string>();
        }

        private void ServiceDiscovered(object sender, ResonanceDiscoveredServiceEventArgs<ResonanceUdpDiscoveredService<DiscoveryInfo>, DiscoveryInfo> e)
        {
            InvokeUI(() =>
            {
                DiscoveredServices.Add(e.DiscoveredService);
            });
        }

        private void ServiceLost(object sender, ResonanceDiscoveredServiceEventArgs<ResonanceUdpDiscoveredService<DiscoveryInfo>, DiscoveryInfo> e)
        {
            InvokeUI(() =>
            {
                DiscoveredServices.Remove(e.DiscoveredService);
            });
        }

        protected async override void OnApplicationReady()
        {
            base.OnApplicationReady();
            await _discoveryClient.Start();
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
                        .WithTcpAdapter()
                        .WithAddress(SelectedService.Address)
                        .WithPort(SelectedService.DiscoveryInfo.Port)
                        .WithJsonTranscoding()
                        .Build();

                    _transporter.ConnectionLost += ConnectionLost;
                    _transporter.RegisterRequestHandler<JoinSessionRequest>(OnJoinSessionRequest);
                    _transporter.RegisterRequestHandler<NotifyAvailableClientsRequest>(OnNotifyAvailableClientsRequest);
                    _transporter.RegisterRequestHandler<EchoTextRequest, EchoTextResponse>(OnEchoTextRequest);
                    _transporter.RegisterRequestHandler<LeaveSessionRequest>(OnLeaveSessionRequest);

                    await _transporter.Connect();
                    await _transporter.SendRequest<LoginRequest, LoginResponse>(new LoginRequest() { ClientID = ClientID });

                    await _discoveryClient.Stop();

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

        private async void JoinSession()
        {
            try
            {
                var response = await _transporter.SendRequest<JoinSessionRequest, JoinSessionResponse>(new JoinSessionRequest()
                {
                    ClientID = SelectedClient
                }, new ResonanceRequestConfig() { Timeout = TimeSpan.FromSeconds(10) });

                InSession = true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.Message);
            }
        }

        private async void LeaveSession()
        {
            await _transporter.SendObject(new LeaveSessionRequest()
            {

            });

            InSession = false;
        }

        private async void SendMessage()
        {
            var response = await _transporter.SendRequest<EchoTextRequest, EchoTextResponse>(new EchoTextRequest()
            {
                Message = Message
            });
        }

        private ResonanceActionResult<EchoTextResponse> OnEchoTextRequest(EchoTextRequest request)
        {
            Logger.LogInformation($"{SelectedClient} says: {request.Message}");
            return new EchoTextResponse() { Message = request.Message };
        }

        private async void OnJoinSessionRequest(IResonanceTransporter transporter, ResonanceRequest<JoinSessionRequest> request)
        {
            if (await ShowQuestionMessage($"Client {request.Message.ClientID} wants to create a session. Confirm?"))
            {
                InSession = true;
                SelectedClient = request.Message.ClientID;
                await transporter.SendResponse(new JoinSessionResponse(), request.Token);
            }
            else
            {
                await transporter.SendErrorResponse("No thanks.", request.Token);
            }
        }

        private void OnLeaveSessionRequest(IResonanceTransporter transporter, ResonanceRequest<LeaveSessionRequest> request)
        {
            Logger.LogWarning($"Session lost: {request.Message.Reason}");
            InSession = false;
        }

        private void OnNotifyAvailableClientsRequest(IResonanceTransporter transporter, ResonanceRequest<NotifyAvailableClientsRequest> request)
        {
            InvokeUI(() =>
            {
                ConnectedClients.Clear();

                foreach (var client in request.Message.Clients)
                {
                    ConnectedClients.Add(client);
                }
            });
        }

        private void ClearConnection()
        {
            IsConnected = false;
            InSession = false;

            InvokeUI(() =>
            {
                DiscoveredServices.Clear();
                _discoveryClient?.Start();
            });
        }
    }
}
