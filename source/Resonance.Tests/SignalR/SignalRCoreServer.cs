using Resonance.Tests.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Resonance.Tests.SignalR
{
    public class SignalRCoreServer : ISignalRServer
    {
        private Process cmd;
        private String _address;

        public SignalRCoreServer(String address)
        {
            _address = address;
        }

        public void Start()
        {
            String webApiProjectPath = Path.Combine(TestHelper.GetSolutionFolder(), "Resonance.Tests.SignalRCore.WebAPI");

            cmd = new Process();
            cmd.StartInfo.WorkingDirectory = webApiProjectPath;
            cmd.StartInfo.FileName = "dotnet";
            cmd.StartInfo.Arguments = "run";
            cmd.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            cmd.StartInfo.CreateNoWindow = true;
            cmd.Start();

            Thread.Sleep(4000);

            String expected = "Resonance SignalR Core Unit Test Feedback Service";
            String result = String.Empty;

            while (result != expected)
            {
                try
                {
                    using (HttpClient http = new HttpClient())
                    {
                        result = http.GetStringAsync(_address + "/home").GetAwaiter().GetResult();
                    }
                }
                catch { }

                Thread.Sleep(1000);
            }
        }

        public void Stop()
        {
            try
            {
                cmd?.Kill();
            }
            catch { }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
