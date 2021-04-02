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
using System.Linq;
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
            SignalRMode mode = SignalRMode.Legacy;

            SignalRServer server = new SignalRServer(hostUrl);
            server.Start();

            TestCredentials credentials = new TestCredentials() { Name = "Test" };
            TestServiceInformation serviceInfo = new TestServiceInformation() { ServiceId = "My Test Service" };

            var registeredService = ResonanceServiceFactory.Default.RegisterService<TestCredentials, TestServiceInformation, TestAdapterInformation>(credentials, serviceInfo, hubUrl, mode).GetAwaiter().GetResult();

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

            var remoteServices = ResonanceServiceFactory.Default.GetAvailableServices<TestCredentials, TestServiceInformation>(credentials, hubUrl, mode).GetAwaiter().GetResult();

            Assert.IsTrue(remoteServices.Count == 1);

            var remoteService = remoteServices.First();

            Assert.AreEqual(remoteService.ServiceId, registeredService.ServiceInformation.ServiceId);

            ResonanceJsonTransporter clientTransporter = new ResonanceJsonTransporter(new SignalRAdapter<TestCredentials>(credentials, hubUrl, remoteService.ServiceId, mode));

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

            for (int i = 0; i < 1000; i++)
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

            Assert.IsTrue(percentageOfOutliers < 10, $"Request/Response duration measurements contains {percentageOfOutliers}% outliers and is considered a performance issue.");
        }
    }
}
