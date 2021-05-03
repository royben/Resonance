using Microsoft.VisualStudio.TestTools.UnitTesting;
using Resonance.Adapters.InMemory;
using Resonance.Tests.Common;
using Resonance.Messages;
using Resonance.Transporters;
using System;
using Resonance.Exceptions;
using System.Threading.Tasks;

namespace Resonance.Tests
{
    [TestClass]
    [TestCategory("Services")]
    public class Services_TST : ResonanceTest
    {
        [TestMethod]
        public void Service_Handles_Request_And_Get_Notified_About_State_Changes()
        {
            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            ResonanceJsonTransporter t2 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));

            t1.Connect();
            t2.Connect();

            var testService = new TestService();

            t2.RegisterService(testService);

            var request = new CalculateRequest() { A = 10, B = 15 };
            var response = t1.SendRequest<CalculateRequest, CalculateResponse>(request);

            Assert.AreEqual(response.Sum, request.A + request.B);

            t2.UnregisterService(testService);

            Assert.ThrowsException<TimeoutException>(() =>
            {
                response = t1.SendRequest<CalculateRequest, CalculateResponse>(request, new ResonanceRequestConfig()
                {
                    Timeout = TimeSpan.FromSeconds(1)
                });
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

        [TestMethod]
        public void Service_Handles_Request_And_Reports_Error_By_Throwing_Exception()
        {


            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            ResonanceJsonTransporter t2 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));

            t1.Connect();
            t2.Connect();

            var testErrorService = new TestErrorService();

            t2.RegisterService(testErrorService);

            Assert.ThrowsException<ResonanceResponseException>(() =>
            {
                try
                {
                    var request = new CalculateRequest() { A = 10, B = 15 };
                    var response = t1.SendRequest<CalculateRequest, CalculateResponse>(request);
                }
                catch (Exception ex)
                {
                    Assert.IsTrue(ex.Message == "Test Error Message");
                    throw;
                }
            });

            t1.Dispose(true);
            t2.Dispose(true);
        }

        private class TestErrorService : IResonanceService
        {
            public ResonanceActionResult<CalculateResponse> Calculate(CalculateRequest request)
            {
                throw new Exception("Test Error Message");
            }

            public void OnTransporterStateChanged(ResonanceComponentState state)
            {

            }
        }

        [TestMethod]
        public void Service_Handles_Task_Result()
        {
            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            ResonanceJsonTransporter t2 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));

            t1.Connect();
            t2.Connect();

            var testService = new TestAsyncService();

            t2.RegisterService(testService);

            var request = new CalculateRequest() { A = 10, B = 15 };
            var response = t1.SendRequest<CalculateRequest, CalculateResponse>(request);

            Assert.AreEqual(response.Sum, request.A + request.B);

            t2.UnregisterService(testService);

            t1.Dispose(true);
            t2.Dispose(true);
        }

        private class TestAsyncService : IResonanceService
        {
            public Task<ResonanceActionResult<CalculateResponse>> Calculate(CalculateRequest request)
            {
                return Task.Factory.StartNew<ResonanceActionResult<CalculateResponse>>(() => 
                {
                    return new CalculateResponse() { Sum = request.A + request.B };
                });
            }

            public void OnTransporterStateChanged(ResonanceComponentState state)
            {

            }
        }
    }
}
