using Microsoft.Extensions.Logging;
using Resonance.Adapters.Bluetooth;
using Resonance.Example.Bluetooth.Common;
using Resonance.Examples.Common.Logging;
using System.Collections.ObjectModel;

namespace Resonance.Examples.Bluetooth.MauiClient
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

        private bool _isBusy;
        public bool IsBusy
        {
            get { return _isBusy; }
            set { _isBusy = value; RaisePropertyChangedAuto(); }
        }

        private String _busyMessage;
        public String BusyMessage
        {
            get { return _busyMessage; }
            set { _busyMessage = value; RaisePropertyChangedAuto(); }
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

        /// <summary>
        /// Android 12 (API 31) replaced the install-time BLUETOOTH/BLUETOOTH_ADMIN permissions
        /// with the runtime BLUETOOTH_SCAN and BLUETOOTH_CONNECT permissions. Discovery silently
        /// returns nothing without them, so request before every scan.
        /// </summary>
        private async Task<bool> EnsureBluetoothPermissionAsync()
        {
            var status = await Permissions.CheckStatusAsync<Permissions.Bluetooth>();

            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.Bluetooth>();
            }

            if (status != PermissionStatus.Granted)
            {
                await ShowMessage("Bluetooth permission is required to discover and connect to devices.");
                return false;
            }

            return true;
        }

        private async void Discover()
        {
            if (!await EnsureBluetoothPermissionAsync()) return;

            try
            {
                Busy("Discovering devices...");

                Devices.Clear();

                var devices = await BluetoothAdapter.DiscoverDevicesAsync(5);

                foreach (var device in devices)
                {
                    Devices.Add(device);
                }
            }
            catch (Exception ex)
            {
                await ShowMessage($"Error discovering devices.\n{ex.Message}");
            }
            finally
            {
                NotBusy();
            }
        }

        private async void Connect()
        {
            if (!await EnsureBluetoothPermissionAsync()) return;

            try
            {
                Busy("Connecting...");

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
                            await ShowMessage($"Connection lost.\n{_transporter.FailedStateException}");
                        }
                    });
                };

                _transporter.RegisterRequestHandler<ChatMessageRequest, ChatMessageResponse>(OnChatRequest);

                await _transporter.ConnectAsync();

                SelectedDevice.Refresh();
                Logs.Clear();

                NotBusy();

                await Navigation.PushAsync(new ChatPage(), true);
            }
            catch (Exception ex)
            {
                await ShowMessage($"Error connecting to the selected device.\n{ex.Message}");
            }
            finally
            {
                NotBusy();
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
                await ShowMessage($"Error sending message.\n{ex.Message}");
            }
        }

        public async void Disconnect()
        {
            if (_transporter != null)
            {
                await _transporter.DisconnectAsync();
            }
        }

        private void Busy(String message)
        {
            InvokeUI(() =>
            {
                BusyMessage = message;
                IsBusy = true;
            });
        }

        private void NotBusy()
        {
            InvokeUI(() => IsBusy = false);
        }

        private Task ShowMessage(String message)
        {
            var page = Application.Current?.Windows.FirstOrDefault()?.Page;
            return page == null ? Task.CompletedTask : page.DisplayAlert("Resonance", message, "OK");
        }

        private void InvokeUI(Action action)
        {
            MainThread.BeginInvokeOnMainThread(action);
        }

        private void LoggingConfiguration_LogReceived(object sender, LogReceivedEventArgs e)
        {
            LogEventVM logVM = new LogEventVM(e.LogEvent, e.FormatProvider);
            InvokeUI(() => { Logs.Add(logVM); });
        }
    }
}
