using Microsoft.VisualStudio.TestTools.UnitTesting;
using Resonance.Adapters.InMemory;
using Resonance.Tests.Common;
using Resonance.Messages;
using Resonance.Transporters;
using System;
using System.Threading.Tasks;
using System.Threading;
using Resonance.Exceptions;

namespace Resonance.Tests
{
    [TestClass]
    [TestCategory("Message Handlers")]
    public class Handlers_TST : ResonanceTest
    {
        [TestMethod]
        public void Message_Handler()
        {
            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            ResonanceJsonTransporter t2 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));

            t1.Connect();
            t2.Connect();

            bool received = false;

            t2.RegisterMessageHandler<CalculateRequest>((t, message) =>
            {
                received = true;
            });

            t1.Send(new CalculateRequest() { A = 10, B = 15 });

            Thread.Sleep(500);

            Assert.IsTrue(received);

            t1.Dispose(true);
            t2.Dispose(true);
        }

        [TestMethod]
        public void Message_Handler_ACK()
        {
            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            ResonanceJsonTransporter t2 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));

            t1.Connect();
            t2.Connect();

            bool received = false;

            t2.RegisterMessageHandler<CalculateRequest>((t, message) =>
            {
                received = true;
            });

            t1.Send(new CalculateRequest() { A = 10, B = 15 }, new ResonanceMessageConfig() { RequireACK = true });

            Thread.Sleep(500);

            Assert.IsTrue(received);

            t1.Dispose(true);
            t2.Dispose(true);
        }

        [TestMethod]
        public void Message_Handler_ACK_Throws_Exception()
        {
            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            ResonanceJsonTransporter t2 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));

            t2.MessageAcknowledgmentBehavior = ResonanceMessageAckBehavior.ReportErrors;
            t1.MessageAcknowledgmentBehavior = ResonanceMessageAckBehavior.ReportErrors;

            t1.Connect();
            t2.Connect();

            bool received = false;

            t2.RegisterMessageHandler<CalculateRequest>((t, message) =>
            {
                received = true;
                throw new Exception("Test Error");
            });

            Assert.ThrowsException<ResonanceResponseException>(() =>
            {
                t1.Send(new CalculateRequest() { A = 10, B = 15 }, new ResonanceMessageConfig() { RequireACK = true });
            });

            Thread.Sleep(500);

            Assert.IsTrue(received);

            t1.Dispose(true);
            t2.Dispose(true);
        }

        [TestMethod]
        public void Standard_Request_Handler()
        {


            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            ResonanceJsonTransporter t2 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));

            t1.Connect();
            t2.Connect();

            t2.RegisterRequestHandler<CalculateRequest>(CalculateRequest_Standard_Handler);

            var request = new CalculateRequest() { A = 10, B = 15 };
            var response = t1.SendRequest<CalculateRequest, CalculateResponse>(request);

            t1.Dispose(true);
            t2.Dispose(true);

            Assert.AreEqual(response.Sum, request.A + request.B);
        }

        [TestMethod]
        public void Request_Response_Handler()
        {


            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            ResonanceJsonTransporter t2 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));

            t1.Connect();
            t2.Connect();

            t2.RegisterRequestHandler<CalculateRequest, CalculateResponse>(CalculateRequest_Response_Handler);

            var request = new CalculateRequest() { A = 10, B = 15 };
            var response = t1.SendRequest<CalculateRequest, CalculateResponse>(request);

            t1.Dispose(true);
            t2.Dispose(true);

            Assert.AreEqual(response.Sum, request.A + request.B);
        }

        private ResonanceActionResult<CalculateResponse> CalculateRequest_Response_Handler(CalculateRequest request)
        {
            return new CalculateResponse() { Sum = request.A + request.B };
        }

        private void CalculateRequest_Standard_Handler(IResonanceTransporter transporter, ResonanceMessage<CalculateRequest> request)
        {
            transporter.SendResponseAsync(new CalculateResponse() { Sum = request.Object.A + request.Object.B }, request.Token);
        }
    }
}
