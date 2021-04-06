using Microsoft.VisualStudio.TestTools.UnitTesting;
using Resonance.Adapters.SignalR;
using Resonance.Messages;
using Resonance.SignalR;
using Resonance.SignalR.Services;
using Resonance.Tests.Common;
using Resonance.Tests.SignalR;
using Resonance.Transporters;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Resonance.Tests
{
    [TestClass]
    [TestCategory("SignalR")]
    public class SignalR_TST : ResonanceTest
    {
        [TestMethod]
        public void SignalR_Legacy_Reading_Writing()
        {
            Init();

            if (IsRunningOnAzurePipelines) return;

            String hostUrl = "http://localhost:8080";
            String hubUrl = $"{hostUrl}/TestHub";

            SignalRServer server = new SignalRServer(hostUrl);
            server.Start();

            SignalR_Reading_Writing(hubUrl, SignalRMode.Legacy);
        }

        [TestMethod]
        public void SignalR_Core_Reading_Writing()
        {
            Init();

            if (IsRunningOnAzurePipelines) return;

            String webApiProjectPath = Path.Combine(TestHelper.GetSolutionFolder(), "Resonance.Tests.SignalRCore.WebAPI");

            Process cmd = new Process();
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
                        result = http.GetStringAsync("http://localhost:27210/home").GetAwaiter().GetResult();
                    }
                }
                catch { }

                Thread.Sleep(1000);
            }

            SignalR_Reading_Writing("http://localhost:27210/hubs/TestHub", SignalRMode.Core);

            cmd.Kill();
        }

        private void SignalR_Reading_Writing(String url, SignalRMode mode)
        {
            TestCredentials credentials = new TestCredentials() { Name = "Test" };
            TestServiceInformation serviceInfo = new TestServiceInformation() { ServiceId = "My Test Service" };

            var registeredService = ResonanceServiceFactory.Default.RegisterService<TestCredentials, TestServiceInformation, TestAdapterInformation>(credentials, serviceInfo, url, mode).GetAwaiter().GetResult();

            Assert.AreSame(registeredService.Credentials, credentials);
            Assert.AreSame(registeredService.ServiceInformation, serviceInfo);
            Assert.AreEqual(registeredService.Mode, mode);

            bool connected = false;

            ResonanceJsonTransporter serviceTransporter = new ResonanceJsonTransporter();

            registeredService.ConnectionRequest += (_, e) =>
            {
                Assert.IsTrue(e.RemoteAdapterInformation.Information == "No information on the remote adapter");
                var adapter = e.Accept().GetAwaiter().GetResult();
                serviceTransporter.Adapter = adapter;
                serviceTransporter.Connect().GetAwaiter().GetResult();
                connected = true;
            };

            var remoteServices = ResonanceServiceFactory.Default.GetAvailableServices<TestCredentials, TestServiceInformation>(credentials, url, mode).GetAwaiter().GetResult();

            Assert.IsTrue(remoteServices.Count == 1);

            var remoteService = remoteServices.First();

            Assert.AreEqual(remoteService.ServiceId, registeredService.ServiceInformation.ServiceId);

            ResonanceJsonTransporter clientTransporter = new ResonanceJsonTransporter(new SignalRAdapter<TestCredentials>(credentials, url, remoteService.ServiceId, mode));

            clientTransporter.Connect().GetAwaiter().GetResult();

            while (!connected)
            {
                Thread.Sleep(100);
            }

            clientTransporter.RequestReceived += (s, e) =>
            {
                CalculateRequest receivedRequest = e.Request.Message as CalculateRequest;
                clientTransporter.SendResponse(new CalculateResponse() { Sum = receivedRequest.A + receivedRequest.B }, e.Request.Token);
            };

            Stopwatch watch = new Stopwatch();

            List<double> measurements = new List<double>();

            for (int i = 0; i < 10; i++)
            {
                watch.Restart();

                var request = new CalculateRequest() { A = 10, B = i };
                var response = serviceTransporter.SendRequest<CalculateRequest, CalculateResponse>(request).GetAwaiter().GetResult();

                measurements.Add(watch.ElapsedMilliseconds);

                Assert.AreEqual(response.Sum, request.A + request.B);
            }

            watch.Stop();

            serviceTransporter.Dispose(true);
            clientTransporter.Dispose(true);

            var outliers = TestHelper.GetOutliers(measurements);

            double percentageOfOutliers = outliers.Count / (double)measurements.Count * 100d;

            Assert.IsTrue(percentageOfOutliers < 20, $"Request/Response duration measurements contains {percentageOfOutliers}% outliers and is considered a performance issue.");
        }
    }
}
