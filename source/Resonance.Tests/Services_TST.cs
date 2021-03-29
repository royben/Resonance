using Microsoft.VisualStudio.TestTools.UnitTesting;
using Resonance.Adapters.InMemory;
using Resonance.Tests.Common;
using Resonance.Messages;
using Resonance.Transporters;
using System;

namespace Resonance.Tests
{
    [TestClass]
    [TestCategory("Services")]
    public class Services_TST : ResonanceTest
    {
        [TestMethod]
        public void Service_Handles_Request_And_Get_Notified_About_State_Changes()
        {
            Init();

            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            ResonanceJsonTransporter t2 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));

            t1.Connect().Wait();
            t2.Connect().Wait();

            var testService = new TestService();

            t2.RegisterService(testService);

            var request = new CalculateRequest() { A = 10, B = 15 };
            var response = t1.SendRequest<CalculateRequest, CalculateResponse>(request).GetAwaiter().GetResult();

            Assert.AreEqual(response.Sum, request.A + request.B);

            t2.UnregisterService(testService);

            Assert.ThrowsException<TimeoutException>(() =>
            {
                response = t1.SendRequest<CalculateRequest, CalculateResponse>(request,new ResonanceRequestConfig() 
                {
                    Timeout = TimeSpan.FromSeconds(1) 
                }).GetAwaiter().GetResult();
            });

            t2.RegisterService(testService);

            t1.Dispose(true);
            t2.Dispose(true);

            Assert.IsTrue(testService.State == ResonanceComponentState.Disposed);
        }

        public class TestService : IResonanceService
        {
            public ResonanceComponentState State { get; set; }

            public ResonanceActionResult<CalculateResponse> Calculate(CalculateRequest request)
            {
                return new CalculateResponse() { Sum = request.A + request.B };
            }

            public void OnTransporterStateChanged(ResonanceComponentState state)
            {
                State = state;
            }
        }
    }
}
