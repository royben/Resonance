using Microsoft.Extensions.Logging;
using Resonance.Adapters.Tcp;
using Resonance.Adapters.Udp;
using Resonance.Examples.Common;
using Resonance.Examples.Common.Logging;
using Resonance.Examples.RPC.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Resonance.Examples.RPC.Client
{
    public class MainWindowVM : ResonanceViewModel
    {
        private IResonanceTransporter _transporter;
        private IRemoteDrawingBoardService _client;

        public ObservableCollection<RemoteRect> Rectangles { get; set; }

        public ObservableCollection<LogEventVM> Logs { get; set; }

        public RelayCommand<RemotePoint> StartRectangleCommand { get; set; }

        public RelayCommand<RemoteRect> SizeRectangleCommand { get; set; }

        public RelayCommand<RemoteRect> FinishRectangleCommand { get; set; }

        public MainWindowVM()
        {
            Rectangles = new ObservableCollection<RemoteRect>();
            Logs = new ObservableCollection<LogEventVM>();

            StartRectangleCommand = new RelayCommand<RemotePoint>(RectangleStarted);
            SizeRectangleCommand = new RelayCommand<RemoteRect>(RectangleResized);
            FinishRectangleCommand = new RelayCommand<RemoteRect>(RectangleFinished);

            LoggingConfiguration.LogReceived += LoggingConfiguration_LogReceived;
        }

        private void LoggingConfiguration_LogReceived(object sender, LogReceivedEventArgs e)
        {
            InvokeUI(() => Logs.Insert(0, new LogEventVM(e.LogEvent, e.FormatProvider)));
        }

        protected async override void OnApplicationReady()
        {
            base.OnApplicationReady();

            _transporter = ResonanceTransporter.Builder
                .Create()
                .WithUdpAdapter()
                .WithServer(TcpAdapter.GetLocalIPAddress(), 9999)
                .WithJsonTranscoding()
                .Build();

            _transporter.ConnectionLost += (x, e) =>
            {
                e.FailTransporter = true;
                Logger.LogError($"Remote server has closed. {e.Exception.Message}");
            };

            _client = _transporter.CreateClientProxy<IRemoteDrawingBoardService>();
            _client.RectangleAdded += _client_RectangleAdded;

            try
            {
                await _transporter.ConnectAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Could not connect to the remote server. Please restart the application.");
                return;
            }

            //This is just for testing async methods...
            String welcomeMessage = await _client.GetWelcomeMessage("Roy", 99);
            int countAsync = await _client.GetRectanglesCountAsync();
            int sum = await _client.CalcAsync(10, 15);

            try
            {
                foreach (var rect in _client.Rectangles)
                {
                    Rectangles.Add(rect);
                }
            }
            catch (Exception ex)
            {
                //TODO: Add log here that we could not fetch any server logs or something.
            }
        }

        protected override void OnApplicationShutdown()
        {
            base.OnApplicationShutdown();

            if (_transporter.State == ResonanceComponentState.Connected)
            {
                _transporter.Dispose();
            }
        }

        private void _client_RectangleAdded(object sender, RemoteRectAddedEventArgs e)
        {
            if (!Rectangles.Contains(e.Rect)) //Check if we have not received the same rect that we have just finished...
            {
                InvokeUI(() =>
                {
                    Rectangles.Add(e.Rect);
                });
            }
        }

        private void RectangleStarted(RemotePoint position)
        {
            if (_transporter.State == ResonanceComponentState.Connected)
            {
                _client.StartRectangle(position);
            }
        }

        private void RectangleResized(RemoteRect size)
        {
            if (_transporter.State == ResonanceComponentState.Connected)
            {
                _client.SizeRectangle(size);
            }
        }

        private void RectangleFinished(RemoteRect rect)
        {
            if (_transporter.State == ResonanceComponentState.Connected)
            {
                _client.FinishRectangle(rect);
            }
        }
    }
}
