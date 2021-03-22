using System;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Resonance.Adapters;
using Resonance.Transporters;

namespace Resonance.Tests
{
    [TestClass]
    [TestCategory("Transporters")]
    public class Transporters_TST
    {
        public class CalculateRequest
        {
            public double A { get; set; }
            public double B { get; set; }
        }

        public class CalculateResponse
        {
            public double Sum { get; set; }
        }

        [TestMethod]
        public void _Send_And_Receive()
        {
            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            ResonanceJsonTransporter t2 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));

            t1.Connect().Wait();
            t2.Connect().Wait();

            t2.RequestReceived += (s, e) =>
            {
                CalculateRequest receivedRequest = e.Request.Message as CalculateRequest;
                t2.SendResponse(new CalculateResponse() { Sum = receivedRequest.A + receivedRequest.B }, e.Request.Token);
            };

            var request = new CalculateRequest() { A = 10, B = 15 };
            var response = t1.SendRequest<CalculateRequest, CalculateResponse>(request).GetAwaiter().GetResult();

            t1.Dispose(true);
            t2.Dispose(true);

            Thread.Sleep(1000);

            Assert.AreEqual(response.Sum, request.A + request.B);
        }
    }
}
