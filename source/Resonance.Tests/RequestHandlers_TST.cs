using Microsoft.VisualStudio.TestTools.UnitTesting;
using Resonance.Adapters.InMemory;
using Resonance.Tests.Common;
using Resonance.Messages;
using Resonance.Transporters;
using System;
using System.Threading.Tasks;

namespace Resonance.Tests
{
    [TestClass]
    [TestCategory("Request Handlers")]
    public class RequestHandlers_TST : ResonanceTest
    {
        [TestMethod]
        public void Standard_Request_Handler()
        {


            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            ResonanceJsonTransporter t2 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));

            t1.Connect().GetAwaiter().GetResult();
            t2.Connect().GetAwaiter().GetResult();

            t2.RegisterRequestHandler<CalculateRequest>(CalculateRequest_Standard_Handler);

            var request = new CalculateRequest() { A = 10, B = 15 };
            var response = t1.SendRequest<CalculateRequest, CalculateResponse>(request).GetAwaiter().GetResult();

            t1.Dispose(true);
            t2.Dispose(true);

            Assert.AreEqual(response.Sum, request.A + request.B);
        }

        [TestMethod]
        public void Request_Response_Handler()
        {


            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            ResonanceJsonTransporter t2 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));

            t1.Connect().GetAwaiter().GetResult();
            t2.Connect().GetAwaiter().GetResult();

            t2.RegisterRequestHandler<CalculateRequest, CalculateResponse>(CalculateRequest_Response_Handler);

            var request = new CalculateRequest() { A = 10, B = 15 };
            var response = t1.SendRequest<CalculateRequest, CalculateResponse>(request).GetAwaiter().GetResult();

            t1.Dispose(true);
            t2.Dispose(true);

            Assert.AreEqual(response.Sum, request.A + request.B);
        }

        private ResonanceActionResult<CalculateResponse> CalculateRequest_Response_Handler(CalculateRequest request)
        {
            return new CalculateResponse() { Sum = request.A + request.B };
        }

        private void CalculateRequest_Standard_Handler(IResonanceTransporter transporter, ResonanceRequest<CalculateRequest> request)
        {
            transporter.SendResponse(new CalculateResponse() { Sum = request.Message.A + request.Message.B }, request.Token);
        }
    }
}
