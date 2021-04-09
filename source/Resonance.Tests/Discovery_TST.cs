using Microsoft.VisualStudio.TestTools.UnitTesting;
using Resonance.Adapters.Tcp;
using Resonance.Discovery;
using Resonance.Tests.Common;
using Resonance.Messages;
using Resonance.Transcoding.Json;
using Resonance.Transporters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Resonance.Servers.Tcp;
using System.Threading.Tasks;

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
        public async Task Udp_Discovery()
        {
            Init();

            ResonanceUdpDiscoveryService<DiscoveryInfo, JsonEncoder> service =
                new ResonanceUdpDiscoveryService<DiscoveryInfo, JsonEncoder>(new DiscoveryInfo()
                {
                    Identity = "Test Identity"
                }, 1984);

            await service.Start();

            ResonanceUdpDiscoveryClient<DiscoveryInfo, JsonDecoder> client = new ResonanceUdpDiscoveryClient<DiscoveryInfo, JsonDecoder>(1984);

            var services = await client.Discover(TimeSpan.FromSeconds(10));

            Assert.IsTrue(services.Count == 1);
            Assert.IsTrue(services[0].DiscoveryInfo.Identity == service.DiscoveryInfo.Identity);

            await service.DisposeAsync();
            await client.DisposeAsync();
        }

        [TestMethod]
        public async Task Udp_Multi_Discovery()
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

                await service.Start();
            }

            ResonanceUdpDiscoveryClient<DiscoveryInfo, JsonDecoder> client = new ResonanceUdpDiscoveryClient<DiscoveryInfo, JsonDecoder>(
                1984,
                (s1, s2) => s1.DiscoveryInfo.Identity == s2.DiscoveryInfo.Identity);

            var discoveredDervices = await client .Discover(TimeSpan.FromSeconds(10));

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
        public async Task Udp_Discovery_Service_Lost()
        {
            Init();

            ResonanceUdpDiscoveryService<DiscoveryInfo, JsonEncoder> service =
                new ResonanceUdpDiscoveryService<DiscoveryInfo, JsonEncoder>(new DiscoveryInfo()
                {
                    Identity = "Test Identity"
                }, 1984);

            await service.Start();

            ResonanceUdpDiscoveryClient<DiscoveryInfo, JsonDecoder> client = new ResonanceUdpDiscoveryClient<DiscoveryInfo, JsonDecoder>(1984);

            AutoResetEvent waitHandle = new AutoResetEvent(false);
            bool lost = false;

            client.ServiceLost += (x, e) =>
            {
                lost = true;
                waitHandle.Set();
            };

            var services = await client.Discover(TimeSpan.FromSeconds(10), 1);

            Assert.IsTrue(services.Count == 1);
            Assert.IsTrue(services[0].DiscoveryInfo.Identity == service.DiscoveryInfo.Identity);

            await service.DisposeAsync();

            waitHandle.WaitOne(TimeSpan.FromSeconds(10));

            Assert.IsTrue(lost);

            await client.DisposeAsync();
        }

        [TestMethod]
        public async Task Udp_Discovery_And_Tcp_Transporter_Connection()
        {
            Init();

            ResonanceUdpDiscoveryService<DiscoveryInfoTransporter, JsonEncoder> service =
                new ResonanceUdpDiscoveryService<DiscoveryInfoTransporter, JsonEncoder>(new DiscoveryInfoTransporter()
                {
                    TcpServerPort = 9999
                }, 1984);

            await service.Start();

            ResonanceUdpDiscoveryClient<DiscoveryInfoTransporter, JsonDecoder> client = new ResonanceUdpDiscoveryClient<DiscoveryInfoTransporter, JsonDecoder>(1984);

            var services = await client.Discover(TimeSpan.FromSeconds(10), 1);

            Assert.IsTrue(services.Count == 1);
            Assert.IsTrue(services[0].DiscoveryInfo.TcpServerPort == service.DiscoveryInfo.TcpServerPort);

            await service.DisposeAsync();
            await client.DisposeAsync();

            var discoveredService = services.First();

            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new TcpAdapter(discoveredService.Address, discoveredService.DiscoveryInfo.TcpServerPort));
            ResonanceJsonTransporter t2 = new ResonanceJsonTransporter();

            ResonanceTcpServer server = new ResonanceTcpServer(9999);
            await server.Start();
            server.ConnectionRequest += async (x, e) =>
            {
                t2.Adapter = e.Accept();
                await t2.Connect();
            };

            await t1.Connect();

            while (t2.State != ResonanceComponentState.Connected)
            {
                Thread.Sleep(10);
            }

            t2.RequestReceived += async (s, e) =>
            {
                CalculateRequest receivedRequest = e.Request.Message as CalculateRequest;
                await t2.SendResponse(new CalculateResponse() { Sum = receivedRequest.A + receivedRequest.B }, e.Request.Token);
            };

            var request = new CalculateRequest() { A = 10, B = 15 };
            var response = await t1.SendRequest<CalculateRequest, CalculateResponse>(request);
            Assert.AreEqual(response.Sum, request.A + request.B);

            await t1.DisposeAsync(true);
            await t2.DisposeAsync(true);
            await server.DisposeAsync();
        }
    }
}
