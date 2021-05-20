using Acr.UserDialogs;
using Microsoft.Extensions.Logging;
using Resonance;
using Resonance.Adapters.Bluetooth;
using Resonance.Example.Bluetooth.Common;
using Resonance.Examples.Common.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Input;
using Xamarin.Forms;

namespace XamarinClient
{
    public class ViewModel : ResonanceObject
    {
        private IResonanceTransporter _transporter;

        public INavigation Navigation { get; set; }

        private String _message;
        public String Message
        {
            get { return _message; }
            set { _message = value; RaisePropertyChangedAuto(); }
        }

        public ObservableCollection<LogEventVM> Logs { get; set; }

        public ObservableCollection<BluetoothDevice> Devices { get; set; }

        private BluetoothDevice _selectedDevice;
        public BluetoothDevice SelectedDevice
        {
            get { return _selectedDevice; }
            set
            {
                _selectedDevice = value;
                RaisePropertyChangedAuto();
                ConnectCommand?.ChangeCanExecute();
            }
        }

        public Command DiscoverCommand { get; set; }

        public Command ConnectCommand { get; set; }

        public Command SendCommand { get; set; }

        public ViewModel()
        {
            Message = "Hi Resonance!";

            Devices = new ObservableCollection<BluetoothDevice>();
            Logs = new ObservableCollection<LogEventVM>();

            DiscoverCommand = new Command(Discover);
            ConnectCommand = new Command(Connect, () => SelectedDevice != null);
            SendCommand = new Command(SendMessage, () => _transporter != null && _transporter.State == ResonanceComponentState.Connected);

            LoggingConfiguration.LogReceived += LoggingConfiguration_LogReceived;
        }

        private async void Discover()
        {
            using (UserDialogs.Instance.Loading("Discovering devices..."))
            {
                Devices.Clear();

                var devices = await BluetoothAdapter.DiscoverDevicesAsync(5);

                foreach (var device in devices)
                {
                    Devices.Add(device);
                }
            }
        }

        private async void Connect()
        {
            try
            {
                using (UserDialogs.Instance.Loading("Connecting..."))
                {
                    _transporter = ResonanceTransporter.Builder
                        .Create()
                        .WithAdapter(new BluetoothAdapter(SelectedDevice))
                        .WithJsonTranscoding()
                        .Build();

                    _transporter.StateChanged += (x, e) =>
                    {
                        InvokeUI(async () =>
                        {
                            SendCommand.ChangeCanExecute();

                            if (e.PreviousState == ResonanceComponentState.Connected && e.NewState == ResonanceComponentState.Failed)
                            {
                                await Navigation.PopAsync();
                                UserDialogs.Instance.Toast($"Connection lost.\n{_transporter.FailedStateException}");
                            }
                        });
                    };

                    _transporter.RegisterRequestHandler<ChatMessageRequest, ChatMessageResponse>(OnChatRequest);

                    await _transporter.ConnectAsync();

                    SelectedDevice.Refresh();
                    Logs.Clear();
                }

                await Navigation.PushAsync(new ChatPage(), true);
            }
            catch (Exception ex)
            {
                UserDialogs.Instance.Toast($"Error connecting to the selected device.\n{ex.Message}");
            }
        }

        private ResonanceActionResult<ChatMessageResponse> OnChatRequest(IResonanceTransporter transporter, ChatMessageRequest request)
        {
            Logger.LogInformation($"{(transporter.Adapter as BluetoothAdapter).Device.Name} says: {request.Message}");
            return new ChatMessageResponse() { Message = request.Message };
        }

        private async void SendMessage()
        {
            try
            {
                await _transporter.SendRequestAsync<ChatMessageRequest, ChatMessageResponse>(new ChatMessageRequest()
                {
                    Message = Message
                }, new ResonanceRequestConfig()
                {
                    LoggingMode = ResonanceMessageLoggingMode.Content
                });
            }
            catch (Exception ex)
            {
                UserDialogs.Instance.Toast($"Error sending message.\n{ex.Message}");
            }
        }

        public async void Disconnect()
        {
            await _transporter?.DisconnectAsync();
        }

        private void InvokeUI(Action action)
        {
            Device.BeginInvokeOnMainThread(action);
        }

        private void LoggingConfiguration_LogReceived(object sender, LogReceivedEventArgs e)
        {
            LogEventVM logVM = new LogEventVM(e.LogEvent, e.FormatProvider);
            InvokeUI(() => { Logs.Add(logVM); });
        }
    }
}
