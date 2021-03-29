using Microsoft.VisualStudio.TestTools.UnitTesting;
using Resonance.Adapters.Tcp;
using Resonance.Discovery;
using Resonance.Tcp;
using Resonance.Tests.Common;
using Resonance.Messages;
using Resonance.Transcoding.Json;
using Resonance.Transporters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Resonance.Tests
{
    [TestClass]
    [TestCategory("Discovery")]
    public class Discovery_TST : ResonanceTest
    {
        public class DiscoveryInfo
        {
            public String Identity { get; set; }
        }

        public class DiscoveryInfoTransporter
        {
            public int TcpServerPort { get; set; }
        }

        [TestMethod]
        public void Udp_Discovery()
        {
            Init();

            ResonanceUdpDiscoveryService<DiscoveryInfo, JsonEncoder> service =
                new ResonanceUdpDiscoveryService<DiscoveryInfo, JsonEncoder>(new DiscoveryInfo()
                {
                    Identity = "Test Identity"
                }, 1984);

            service.Start();

            ResonanceUdpDiscoveryClient<DiscoveryInfo, JsonDecoder> client = new ResonanceUdpDiscoveryClient<DiscoveryInfo, JsonDecoder>(1984);

            var services = client.Discover(TimeSpan.FromSeconds(10)).GetAwaiter().GetResult();

            Assert.IsTrue(services.Count == 1);
            Assert.IsTrue(services[0].DiscoveryInfo.Identity == service.DiscoveryInfo.Identity);

            service.Dispose();
            client.Dispose();
        }

        [TestMethod]
        public void Udp_Multi_Discovery()
        {
            Init();

            List<DiscoveryInfo> infos = new List<DiscoveryInfo>();
            List<ResonanceUdpDiscoveryService<DiscoveryInfo, JsonEncoder>> services = new List<ResonanceUdpDiscoveryService<DiscoveryInfo, JsonEncoder>>();

            int servicesCount = 10;

            for (int i = 0; i < servicesCount; i++)
            {
                DiscoveryInfo info = new DiscoveryInfo();
                info.Identity = $"Test Identity {i}";
                infos.Add(info);

                ResonanceUdpDiscoveryService<DiscoveryInfo, JsonEncoder> service =
                new ResonanceUdpDiscoveryService<DiscoveryInfo, JsonEncoder>(info, 1984);

                services.Add(service);

                service.Start();
            }

            ResonanceUdpDiscoveryClient<DiscoveryInfo, JsonDecoder> client = new ResonanceUdpDiscoveryClient<DiscoveryInfo, JsonDecoder>(
                1984,
                (s1, s2) => s1.DiscoveryInfo.Identity == s2.DiscoveryInfo.Identity);

            var discoveredDervices = client.Discover(TimeSpan.FromSeconds(10)).GetAwaiter().GetResult();

            Assert.IsTrue(discoveredDervices.Count == servicesCount);

            var discoveredServicesOrdered = discoveredDervices.OrderBy(x => x.DiscoveryInfo.Identity).ToList();

            for (int i = 0; i < discoveredServicesOrdered.Count; i++)
            {
                Assert.IsTrue(discoveredServicesOrdered[i].DiscoveryInfo.Identity == infos[i].Identity);
            }

            services.ForEach(x => x.Dispose());
            client.Dispose();
        }

        [TestMethod]
        public void Udp_Discovery_Service_Lost()
        {
            Init();

            ResonanceUdpDiscoveryService<DiscoveryInfo, JsonEncoder> service =
                new ResonanceUdpDiscoveryService<DiscoveryInfo, JsonEncoder>(new DiscoveryInfo()
                {
                    Identity = "Test Identity"
                }, 1984);

            service.Start();

            ResonanceUdpDiscoveryClient<DiscoveryInfo, JsonDecoder> client = new ResonanceUdpDiscoveryClient<DiscoveryInfo, JsonDecoder>(1984);

            AutoResetEvent waitHandle = new AutoResetEvent(false);
            bool lost = false;

            client.ServiceLost += (x, e) =>
            {
                lost = true;
                waitHandle.Set();
            };

            var services = client.Discover(TimeSpan.FromSeconds(10), 1).GetAwaiter().GetResult();

            Assert.IsTrue(services.Count == 1);
            Assert.IsTrue(services[0].DiscoveryInfo.Identity == service.DiscoveryInfo.Identity);

            service.Dispose();

            waitHandle.WaitOne(TimeSpan.FromSeconds(10));

            Assert.IsTrue(lost);

            client.Dispose();
        }

        [TestMethod]
        public void Udp_Discovery_And_Tcp_Transporter_Connection()
        {
            Init();

            ResonanceUdpDiscoveryService<DiscoveryInfoTransporter, JsonEncoder> service =
                new ResonanceUdpDiscoveryService<DiscoveryInfoTransporter, JsonEncoder>(new DiscoveryInfoTransporter()
                {
                    TcpServerPort = 9999
                }, 1984);

            service.Start();

            ResonanceUdpDiscoveryClient<DiscoveryInfoTransporter, JsonDecoder> client = new ResonanceUdpDiscoveryClient<DiscoveryInfoTransporter, JsonDecoder>(1984);

            var services = client.Discover(TimeSpan.FromSeconds(10), 1).GetAwaiter().GetResult();

            Assert.IsTrue(services.Count == 1);
            Assert.IsTrue(services[0].DiscoveryInfo.TcpServerPort == service.DiscoveryInfo.TcpServerPort);

            service.Dispose();
            client.Dispose();

            var discoveredService = services.First();

            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new TcpAdapter(discoveredService.Address, discoveredService.DiscoveryInfo.TcpServerPort));
            ResonanceJsonTransporter t2 = new ResonanceJsonTransporter();

            ResonanceTcpServer server = new ResonanceTcpServer(9999);
            server.Start();
            server.ClientConnected += (x, e) =>
            {
                t2.Adapter = new TcpAdapter(e.TcpClient);
                t2.Connect().Wait();
            };

            t1.Connect().Wait();

            while (t2.State != ResonanceComponentState.Connected)
            {
                Thread.Sleep(10);
            }

            t2.RequestReceived += (s, e) =>
            {
                CalculateRequest receivedRequest = e.Request.Message as CalculateRequest;
                t2.SendResponse(new CalculateResponse() { Sum = receivedRequest.A + receivedRequest.B }, e.Request.Token);
            };

            var request = new CalculateRequest() { A = 10, B = 15 };
            var response = t1.SendRequest<CalculateRequest, CalculateResponse>(request).GetAwaiter().GetResult();
            Assert.AreEqual(response.Sum, request.A + request.B);

            t1.Dispose(true);
            t2.Dispose(true);
            server.Dispose();
        }
    }
}
