using Microsoft.VisualStudio.TestTools.UnitTesting;
using Resonance.Adapters.InMemory;
using Resonance.Messages;
using Resonance.Routing;
using Resonance.Servers.Tcp;
using Resonance.Tests.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Resonance.Tests
{
    [TestClass]
    [TestCategory("Routing")]
    public class Routing_TST : ResonanceTest
    {
        [TestMethod]
        public void Transporters_Router_Standard_Routes_Data()
        {
            IResonanceTransporter receiver1 = null;
            IResonanceTransporter client1 = null;
            IResonanceTransporter receiver2 = null;
            IResonanceTransporter client2 = null;
            TransporterRouter router = null;

            ResonanceTcpServer server = new ResonanceTcpServer(1333);
            server.ConnectionRequest += (x, e) =>
            {
                if (receiver1 == null)
                {
                    receiver1 = ResonanceTransporter.Builder.Create()
                    .WithAdapter(e.Accept())
                    .WithJsonTranscoding()
                    .Build();

                    receiver1.Connect();
                }
                else
                {
                    receiver2 = ResonanceTransporter.Builder.Create()
                    .WithAdapter(e.Accept())
                    .WithJsonTranscoding()
                    .Build();

                    receiver2.Connect();

                    router = new TransporterRouter(receiver1, receiver2, RoutingMode.TwoWay, WritingMode.Standard);
                    router.Bind();
                }
            };
            server.Start();


            client1 = ResonanceTransporter.Builder.Create()
                .WithTcpAdapter()
                .WithAddress("127.0.0.1")
                .WithPort(1333)
                .WithJsonTranscoding()
                .Build();

            client1.Connect();

            client2 = ResonanceTransporter.Builder.Create()
                .WithTcpAdapter()
                .WithAddress("127.0.0.1")
                .WithPort(1333)
                .WithJsonTranscoding()
                .Build();

            client2.Connect();

            client2.RegisterRequestHandler<CalculateRequest, CalculateResponse>((request) =>
            {
                return new CalculateResponse() { Sum = request.A + request.B };
            });

            client1.RegisterRequestHandler<CalculateRequest, CalculateResponse>((request) =>
            {
                return new CalculateResponse() { Sum = request.A + request.B };
            });

            Thread.Sleep(1000);

            receiver1.RequestReceived += (_, __) =>
            {
                Assert.Fail();
            };

            receiver2.RequestReceived += (_, __) =>
            {
                Assert.Fail();
            };

            var response = client1.SendRequest<CalculateRequest, CalculateResponse>(new CalculateRequest()
            {
                A = 10,
                B = 5
            });

            Assert.AreEqual(response.Sum, 15);

            response = client2.SendRequest<CalculateRequest, CalculateResponse>(new CalculateRequest()
            {
                A = 11,
                B = 5
            });

            Assert.AreEqual(response.Sum, 16);

            client1.Dispose();
            client2.Dispose();
            receiver1.Dispose();
            receiver2.Dispose();
            server.Dispose();
            router.Dispose();
        }

        [TestMethod]
        public void Transporters_Router_Direct_Routes_Data()
        {
            IResonanceTransporter receiver1 = null;
            IResonanceTransporter client1 = null;
            IResonanceTransporter receiver2 = null;
            IResonanceTransporter client2 = null;
            TransporterRouter router = null;

            ResonanceTcpServer server = new ResonanceTcpServer(1333);
            server.ConnectionRequest += (x, e) =>
            {
                if (receiver1 == null)
                {
                    receiver1 = ResonanceTransporter.Builder.Create()
                    .WithAdapter(e.Accept())
                    .WithJsonTranscoding()
                    .Build();

                    receiver1.Connect();
                }
                else
                {
                    receiver2 = ResonanceTransporter.Builder.Create()
                    .WithAdapter(e.Accept())
                    .WithJsonTranscoding()
                    .Build();

                    receiver2.Connect();

                    router = new TransporterRouter(receiver1, receiver2, RoutingMode.TwoWay, WritingMode.AdapterDirect);
                    router.Bind();
                }
            };
            server.Start();


            client1 = ResonanceTransporter.Builder.Create()
                .WithTcpAdapter()
                .WithAddress("127.0.0.1")
                .WithPort(1333)
                .WithJsonTranscoding()
                .Build();

            client1.Connect();

            client2 = ResonanceTransporter.Builder.Create()
                .WithTcpAdapter()
                .WithAddress("127.0.0.1")
                .WithPort(1333)
                .WithJsonTranscoding()
                .Build();

            client2.Connect();

            client2.RegisterRequestHandler<CalculateRequest, CalculateResponse>((request) =>
            {
                return new CalculateResponse() { Sum = request.A + request.B };
            });

            client1.RegisterRequestHandler<CalculateRequest, CalculateResponse>((request) =>
            {
                return new CalculateResponse() { Sum = request.A + request.B };
            });

            Thread.Sleep(1000);

            receiver1.RequestReceived += (_, __) =>
            {
                Assert.Fail();
            };

            receiver2.RequestReceived += (_, __) =>
            {
                Assert.Fail();
            };

            var response = client1.SendRequest<CalculateRequest, CalculateResponse>(new CalculateRequest()
            {
                A = 10,
                B = 5
            });

            Assert.AreEqual(response.Sum, 15);

            response = client2.SendRequest<CalculateRequest, CalculateResponse>(new CalculateRequest()
            {
                A = 11,
                B = 5
            });

            Assert.AreEqual(response.Sum, 16);

            client1.Dispose();
            client2.Dispose();
            receiver1.Dispose();
            receiver2.Dispose();
            server.Dispose();
            router.Dispose();
        }

        [TestMethod]
        public void Transporters_Router_Standard_Propagates_Disconnection()
        {
            IResonanceTransporter receiver1 = null;
            IResonanceTransporter client1 = null;
            IResonanceTransporter receiver2 = null;
            IResonanceTransporter client2 = null;
            TransporterRouter router = null;

            ResonanceTcpServer server = new ResonanceTcpServer(1333);
            server.ConnectionRequest += (x, e) =>
            {
                if (receiver1 == null)
                {
                    receiver1 = ResonanceTransporter.Builder.Create()
                    .WithAdapter(e.Accept())
                    .WithJsonTranscoding()
                    .Build();

                    receiver1.Connect();
                }
                else
                {
                    receiver2 = ResonanceTransporter.Builder.Create()
                    .WithAdapter(e.Accept())
                    .WithJsonTranscoding()
                    .Build();

                    receiver2.Connect();

                    router = new TransporterRouter(receiver1, receiver2, RoutingMode.TwoWay, WritingMode.Standard);
                    router.Bind();
                }
            };
            server.Start();


            client1 = ResonanceTransporter.Builder.Create()
                .WithTcpAdapter()
                .WithAddress("127.0.0.1")
                .WithPort(1333)
                .WithJsonTranscoding()
                .Build();

            client1.Connect();

            client2 = ResonanceTransporter.Builder.Create()
                .WithTcpAdapter()
                .WithAddress("127.0.0.1")
                .WithPort(1333)
                .WithJsonTranscoding()
                .Build();

            client2.Connect();

            Thread.Sleep(1000);

            client1.Disconnect();

            Thread.Sleep(500);

            Assert.IsTrue(client1.State == ResonanceComponentState.Disconnected);
            Assert.IsTrue(receiver1.State == ResonanceComponentState.Failed);
            Assert.IsTrue(receiver2.State == ResonanceComponentState.Disconnected);
            Assert.IsTrue(client2.State == ResonanceComponentState.Failed);


            client1.Dispose();
            client2.Dispose();
            receiver1.Dispose();
            receiver2.Dispose();
            server.Dispose();
            router.Dispose();
        }

        [TestMethod]
        public void Transporters_Router_Standard_Propagates_Connection_Loss()
        {
            IResonanceTransporter receiver1 = null;
            IResonanceTransporter client1 = null;
            IResonanceTransporter receiver2 = null;
            IResonanceTransporter client2 = null;
            TransporterRouter router = null;

            ResonanceTcpServer server = new ResonanceTcpServer(1333);
            server.ConnectionRequest += (x, e) =>
            {
                if (receiver1 == null)
                {
                    receiver1 = ResonanceTransporter.Builder.Create()
                    .WithAdapter(e.Accept())
                    .WithJsonTranscoding()
                    .WithKeepAlive(TimeSpan.FromSeconds(1), 1)
                    .Build();

                    receiver1.Connect();
                }
                else
                {
                    receiver2 = ResonanceTransporter.Builder.Create()
                    .WithAdapter(e.Accept())
                    .WithJsonTranscoding()
                    .WithKeepAlive()
                    .Build();

                    receiver2.Connect();

                    router = new TransporterRouter(receiver1, receiver2, RoutingMode.TwoWay, WritingMode.Standard);
                    router.Bind();
                }
            };
            server.Start();


            client1 = ResonanceTransporter.Builder.Create()
                .WithTcpAdapter()
                .WithAddress("127.0.0.1")
                .WithPort(1333)
                .WithJsonTranscoding()
                .Build();

            client1.NotifyOnDisconnect = false;
            client1.KeepAliveConfiguration.EnableAutoResponse = false;

            client1.Connect();

            client2 = ResonanceTransporter.Builder.Create()
                .WithTcpAdapter()
                .WithAddress("127.0.0.1")
                .WithPort(1333)
                .WithJsonTranscoding()
                .WithKeepAlive()
                .Build();

            client2.Connect();

            Thread.Sleep(15000);

            client1.Disconnect();

            Assert.IsTrue(client1.State == ResonanceComponentState.Disconnected);
            Assert.IsTrue(receiver1.State == ResonanceComponentState.Failed);
            Assert.IsTrue(receiver2.State == ResonanceComponentState.Disconnected);
            Assert.IsTrue(client2.State == ResonanceComponentState.Failed);


            client1.Dispose();
            client2.Dispose();
            receiver1.Dispose();
            receiver2.Dispose();
            server.Dispose();
            router.Dispose();
        }
    }
}
