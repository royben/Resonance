using Microsoft.Extensions.Logging;
using Resonance.Examples.Common;
using Resonance.Examples.Common.Logging;
using Resonance.Examples.RPC.Common;
using Resonance.Servers.Udp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Resonance.Examples.RPC.Server
{
    public class MainWindowVM : ResonanceViewModel, IRemoteDrawingBoardService
    {
        private List<IResonanceTransporter> _clients;
        private ResonanceUdpServer _udpServer;

        public event EventHandler<RemoteRectAddedEventArgs> RectangleAdded;

        public ObservableCollection<RemoteRect> Rectangles { get; set; }

        public ObservableCollection<LogEventVM> Logs { get; set; }

        private RemoteRect _currentRect;

        public RemoteRect CurrentRect
        {
            get { return _currentRect; }
            set { _currentRect = value; RaisePropertyChangedAuto(); }
        }

        public MainWindowVM()
        {
            Rectangles = new ObservableCollection<RemoteRect>();
            Logs = new ObservableCollection<LogEventVM>();
            _clients = new List<IResonanceTransporter>();

            LoggingConfiguration.LogReceived += LoggingConfiguration_LogReceived;
        }

        private void LoggingConfiguration_LogReceived(object sender, LogReceivedEventArgs e)
        {
            InvokeUI(() => Logs.Insert(0, new LogEventVM(e.LogEvent, e.FormatProvider)));
        }

        protected async override void OnApplicationReady()
        {
            base.OnApplicationReady();

            _udpServer = new ResonanceUdpServer(9999);
            _udpServer.ConnectionRequest += _udpServer_ConnectionRequest;
            await _udpServer.StartAsync();
        }

        protected override void OnApplicationShutdown()
        {
            base.OnApplicationShutdown();

            foreach (var client in _clients.ToList())
            {
                client.Dispose();
            }

            _udpServer.Dispose();
        }

        private async void _udpServer_ConnectionRequest(object sender, ResonanceListeningServerConnectionRequestEventArgs<Adapters.Udp.UdpAdapter> e)
        {
            var transporter = ResonanceTransporter.Builder
                .Create()
                .WithAdapter(e.Accept())
                .WithJsonTranscoding()
                .Build();

            _clients.Add(transporter);

            transporter.ConnectionLost += (x, ee) =>
            {
                transporter.UnregisterService<IRemoteDrawingBoardService>();
                ee.FailTransporter = true;
                Logger.LogError($"A remote client connection lost. {ee.Exception.Message}");
            };

            transporter.RegisterService<IRemoteDrawingBoardService, MainWindowVM>(this);

            await transporter.ConnectAsync();
        }

        public void StartRectangle(RemotePoint position)
        {
            CurrentRect = new RemoteRect() { X = position.X, Y = position.Y };
        }

        public void SizeRectangle(RemoteRect size)
        {
            CurrentRect = size;
        }

        public void FinishRectangle(RemoteRect rect)
        {
            CurrentRect = new RemoteRect();

            InvokeUI(() =>
            {
                Rectangles.Add(rect);
            });

            RectangleAdded?.Invoke(this, new RemoteRectAddedEventArgs() { Rect = rect });
        }

        public Task<string> GetWelcomeMessage(string str, int a)
        {
            return Task.FromResult($"Hi {str}, {a}");
        }

        public int GetRectanglesCount()
        {
            return Rectangles.Count;
        }

        public Task<int> GetRectanglesCountAsync()
        {
            return Task.FromResult(Rectangles.Count);
        }

        public Task<int> CalcAsync(int a, int b)
        {
            return Task.FromResult(a + b);
        }
    }
}