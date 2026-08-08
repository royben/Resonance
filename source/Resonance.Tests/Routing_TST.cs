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

            Assert.AreEqual(15, response.Sum);

            response = client2.SendRequest<CalculateRequest, CalculateResponse>(new CalculateRequest()
            {
                A = 11,
                B = 5
            });

            Assert.AreEqual(16, response.Sum);

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

            Assert.AreEqual(15, response.Sum);

            response = client2.SendRequest<CalculateRequest, CalculateResponse>(new CalculateRequest()
            {
                A = 11,
                B = 5
            });

            Assert.AreEqual(16, response.Sum);

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

            try
            {
                // Both connections must be accepted and the router bound before the state
                // assertions mean anything. A fixed sleep is enough on a developer machine
                // but not on a slower build agent, where receiver2 could still be null.
                TestHelper.WaitWhile(
                    () => receiver1 == null || receiver2 == null || router == null,
                    TimeSpan.FromSeconds(30));

                client1.Disconnect();

                // Disconnection propagates through the router asynchronously.
                TestHelper.WaitWhile(() => client1.State != ResonanceComponentState.Disconnected, TimeSpan.FromSeconds(30));
                TestHelper.WaitWhile(() => receiver1.State != ResonanceComponentState.Failed, TimeSpan.FromSeconds(30));
                TestHelper.WaitWhile(() => receiver2.State != ResonanceComponentState.Disconnected, TimeSpan.FromSeconds(30));
                TestHelper.WaitWhile(() => client2.State != ResonanceComponentState.Failed, TimeSpan.FromSeconds(30));

                Assert.IsTrue(client1.State == ResonanceComponentState.Disconnected);
                Assert.IsTrue(receiver1.State == ResonanceComponentState.Failed);
                Assert.IsTrue(receiver2.State == ResonanceComponentState.Disconnected);
                Assert.IsTrue(client2.State == ResonanceComponentState.Failed);
            }
            finally
            {
                // Must run even when an assertion fails, otherwise the server keeps port
                // 1333 bound and the next routing test cannot accept its connections.
                client1?.Dispose();
                client2?.Dispose();
                receiver1?.Dispose();
                receiver2?.Dispose();
                server?.Dispose();
                router?.Dispose();
            }
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

            try
            {
                // Wait for both connections to be accepted and the router bound before
                // touching receiver1/receiver2 - dereferencing them while still null was
                // what produced a NullReferenceException here on the build agent.
                TestHelper.WaitWhile(
                    () => receiver1 == null || receiver2 == null || router == null,
                    TimeSpan.FromSeconds(30));

                // receiver1 has auto keep alive response disabled on the client side, so
                // its keep alive must time out and fail the transporter.
                TestHelper.WaitWhile(() => receiver1.State != ResonanceComponentState.Failed, TimeSpan.FromSeconds(60));

                client1.Disconnect();

                TestHelper.WaitWhile(() => client1.State != ResonanceComponentState.Disconnected, TimeSpan.FromSeconds(30));
                TestHelper.WaitWhile(() => receiver2.State != ResonanceComponentState.Disconnected, TimeSpan.FromSeconds(30));
                TestHelper.WaitWhile(() => client2.State != ResonanceComponentState.Failed, TimeSpan.FromSeconds(30));

                Assert.IsTrue(client1.State == ResonanceComponentState.Disconnected);
                Assert.IsTrue(receiver1.State == ResonanceComponentState.Failed);
                Assert.IsTrue(receiver2.State == ResonanceComponentState.Disconnected);
                Assert.IsTrue(client2.State == ResonanceComponentState.Failed);
            }
            finally
            {
                client1?.Dispose();
                client2?.Dispose();
                receiver1?.Dispose();
                receiver2?.Dispose();
                server?.Dispose();
                router?.Dispose();
            }
        }
    }
}
