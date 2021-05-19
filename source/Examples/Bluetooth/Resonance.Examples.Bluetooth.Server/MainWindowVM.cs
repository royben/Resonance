using Microsoft.Extensions.Logging;
using Resonance.Adapters.Bluetooth;
using Resonance.Bluetooth;
using Resonance.Example.Bluetooth.Common;
using Resonance.Examples.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.Examples.Bluetooth.Server
{
    public class MainWindowVM : ResonanceViewModel
    {
        private BluetoothServer _server;

        public ObservableCollection<IResonanceTransporter> ConnectedDevices { get; set; }

        private IResonanceTransporter _selectedDevice;
        public IResonanceTransporter SelectedDevice
        {
            get { return _selectedDevice; }
            set { _selectedDevice = value; RaisePropertyChangedAuto(); InvalidateRelayCommands(); }
        }

        public String Message { get; set; }

        public RelayCommand SendCommand { get; set; }

        public RelayCommand DisconnectCommand { get; set; }

        public MainWindowVM()
        {
            ConnectedDevices = new ObservableCollection<IResonanceTransporter>();
            SendCommand = new RelayCommand(SendMessage,() => SelectedDevice != null);
            DisconnectCommand = new RelayCommand(DisconnectSelectedDevice, () => SelectedDevice != null);
        }

        protected async override void OnApplicationReady()
        {
            base.OnApplicationReady();
            _server = new BluetoothServer();
            _server.ConnectionRequest += _server_ConnectionRequest;
            await _server.StartAsync();
        }

        private async void _server_ConnectionRequest(object sender, ResonanceListeningServerConnectionRequestEventArgs<BluetoothAdapter> e)
        {
            IResonanceTransporter transporter = ResonanceTransporter.Builder
                .Create()
                .WithAdapter(e.Accept())
                .WithJsonTranscoding()
                .Build();

            transporter.StateChanged += (x, ee) => 
            {
                if (ee.NewState != ResonanceComponentState.Connected)
                {
                    InvokeUI(() =>
                    {
                        ConnectedDevices.Remove(transporter);
                    });
                }
            };

            transporter.RegisterRequestHandler<ChatMessageRequest, ChatMessageResponse>(OnChatRequest);

            await transporter.ConnectAsync();

            InvokeUI(() =>
            {
                ConnectedDevices.Add(transporter);
                if (SelectedDevice == null) SelectedDevice = transporter;
            });
        }

        private ResonanceActionResult<ChatMessageResponse> OnChatRequest(IResonanceTransporter transporter, ChatMessageRequest request)
        {
            Logger.LogInformation($"{(transporter.Adapter as BluetoothAdapter).Device.Name} says: {request.Message}");
            return new ChatMessageResponse() { Message = request.Message };
        }

        private async void SendMessage()
        {
            await SelectedDevice.SendRequestAsync<ChatMessageRequest, ChatMessageResponse>(new ChatMessageRequest()
            {
                Message = Message
            }, new ResonanceRequestConfig()
            {
                LoggingMode = ResonanceMessageLoggingMode.Content
            });
        }

        private async void DisconnectSelectedDevice()
        {
            await SelectedDevice?.DisconnectAsync();
        }
    }
}
