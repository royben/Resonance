using Microsoft.VisualStudio.TestTools.UnitTesting;
using Resonance.Adapters.SignalR;
using Resonance.Messages;
using Resonance.SignalR;
using Resonance.SignalR.Discovery;
using Resonance.SignalR.Services;
using Resonance.Tests.Common;
using Resonance.Tests.SignalR;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Resonance.Tests
{
    [TestClass]
    [TestCategory("SignalR")]
    public class SignalR_TST : ResonanceTest
    {
        private String legacyHostUrl = "http://localhost:8080";
        private String legacyHubUrl = "http://localhost:8080/TestHub";

        private String coreHostUrl = "http://localhost:27210";
        private String coreHubUrl = "http://localhost:27210/hubs/TestHub";

        [TestMethod]
        public void SignalR_Legacy_Reading_Writing()
        {
            if (IsRunningOnAzurePipelines) return;

            SignalRServer server = new SignalRServer(legacyHostUrl);
            server.Start();

            SignalR_Reading_Writing(legacyHubUrl, SignalRMode.Legacy);
        }

        [TestMethod]
        public void SignalR_Core_Reading_Writing()
        {
            if (IsRunningOnAzurePipelines) return;

            using (SignalRCoreServer server = new SignalRCoreServer(coreHostUrl))
            {
                server.Start();
                SignalR_Reading_Writing(coreHubUrl, SignalRMode.Core);
            }
        }

        private void SignalR_Reading_Writing(String url, SignalRMode mode)
        {
            TestCredentials credentials = new TestCredentials() { Name = "Test" };
            TestServiceInformation serviceInfo = new TestServiceInformation() { ServiceId = "My Test Service" };

            var registeredService = ResonanceServiceFactory.Default.RegisterService<TestCredentials, TestServiceInformation, TestAdapterInformation>(credentials, serviceInfo, url, mode);

            Assert.AreSame(registeredService.Credentials, credentials);
            Assert.AreSame(registeredService.ServiceInformation, serviceInfo);
            Assert.AreEqual(registeredService.Mode, mode);

            bool connected = false;

            ResonanceTransporter serviceTransporter = new ResonanceTransporter();

            registeredService.ConnectionRequest += (_, e) =>
            {
                Assert.IsTrue(e.RemoteAdapterInformation.Information == "No information on the remote adapter");
                serviceTransporter.Adapter = e.Accept();
                serviceTransporter.Connect();
                connected = true;
            };

            var remoteServices = ResonanceServiceFactory.Default.GetAvailableServices<TestCredentials, TestServiceInformation>(credentials, url, mode);

            Assert.IsTrue(remoteServices.Count == 1);

            var remoteService = remoteServices.First();

            Assert.AreEqual(remoteService.ServiceId, registeredService.ServiceInformation.ServiceId);

            ResonanceTransporter clientTransporter = new ResonanceTransporter(new SignalRAdapter<TestCredentials>(credentials, url, remoteService.ServiceId, mode));

            clientTransporter.Connect();

            TestHelper.WaitWhile(() => !connected, TimeSpan.FromSeconds(30));

            TestUtils.Read_Write_Test(this, serviceTransporter, clientTransporter, false, 1000, 20);
        }


        [TestMethod]
        public void SignalR_Legacy_Adapter_Fails_On_Client_Error()
        {
            if (IsRunningOnAzurePipelines) return;

            SignalR_Adapter_Fails_On_Client_Error(new SignalRServer(legacyHostUrl), legacyHostUrl, legacyHubUrl, SignalRMode.Legacy);
        }

        [TestMethod]
        public void SignalR_Core_Adapter_Fails_On_Client_Error()
        {
            if (IsRunningOnAzurePipelines) return;

            SignalR_Adapter_Fails_On_Client_Error(new SignalRCoreServer(coreHostUrl), coreHostUrl, coreHubUrl, SignalRMode.Core);
        }

        private void SignalR_Adapter_Fails_On_Client_Error(ISignalRServer server, String hostUrl, String hubUrl, SignalRMode mode)
        {
            server.Start();

            var registeredService = ResonanceServiceFactory.Default.RegisterService<
                TestCredentials,
                TestServiceInformation,
                TestAdapterInformation>(
                new TestCredentials() { Name = "Service User" },
                new TestServiceInformation() { ServiceId = "Service 1" },
                hubUrl,
                mode);

            bool connected = false;
            IResonanceAdapter serviceAdapter = null;


            registeredService.ConnectionRequest += (x, e) =>
            {
                serviceAdapter = e.Accept();
                serviceAdapter.Connect();
                connected = true;
            };

            SignalRAdapter<TestCredentials> adapter = new SignalRAdapter<TestCredentials>(
                new TestCredentials() { Name = "Test User" },
                hubUrl, "Service 1", mode);

            adapter.Connect();

            while (!connected)
            {
                Thread.Sleep(10);
            }

            Thread.Sleep(1000);

            server.Dispose();
            serviceAdapter.Dispose();

            TestHelper.WaitWhile(() => adapter.State != ResonanceComponentState.Failed, TimeSpan.FromSeconds(10));

            Assert.IsTrue(adapter.State == ResonanceComponentState.Failed);

            Assert.IsInstanceOfType(adapter.FailedStateException, typeof(WebSocketException));
        }


        [TestMethod]
        public void SignalR_Discovery()
        {
            if (IsRunningOnAzurePipelines) return;

            SignalRServer server = new SignalRServer(legacyHostUrl);
            server.Start();

            var registeredService = ResonanceServiceFactory.Default.RegisterService<
                TestCredentials,
                TestServiceInformation,
                TestAdapterInformation>(
                new TestCredentials() { Name = "Service User" },
                new TestServiceInformation() { ServiceId = "Service 1" },
                legacyHubUrl,
                SignalRMode.Legacy);

            ResonanceSignalRDiscoveryClient<TestServiceInformation, TestCredentials> discoveryClient = 
                new ResonanceSignalRDiscoveryClient<TestServiceInformation, TestCredentials>(
                    legacyHubUrl, 
                    SignalRMode.Legacy, 
                    new TestCredentials() { Name = "Test User" });


            var discoveredServices = discoveryClient.Discover(TimeSpan.FromSeconds(10), 1);

            Assert.IsTrue(discoveredServices.Count == 1);
            Assert.IsTrue(discoveredServices[0].DiscoveryInfo.ServiceId == "Service 1");

            registeredService.Dispose();

            discoveredServices = discoveryClient.Discover(TimeSpan.FromSeconds(5), 1);

            Assert.IsTrue(discoveredServices.Count == 0);

            server.Dispose();
        }
    }
}
