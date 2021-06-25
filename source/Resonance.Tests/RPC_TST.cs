using Microsoft.VisualStudio.TestTools.UnitTesting;
using Resonance.Adapters.InMemory;
using Resonance.Tests.Common;
using Resonance.Messages;

using System;
using Resonance.Exceptions;
using System.Threading.Tasks;
using System.Threading;
using Resonance.RPC;
using System.Collections.Generic;

namespace Resonance.Tests
{
    [TestClass]
    [TestCategory("RPC")]
    public class RPC_TST : ResonanceTest
    {
        private static CalculateRequest receivedRequest;

        public static void Reset()
        {
            receivedRequest = new CalculateRequest();
            TestService.InstanceCount = 0;
        }

        public override void Init()
        {
            base.Init();
            Reset();
        }

        public class CalculateResponseEventArgs : EventArgs
        {
            public CalculateResponse Response { get; set; }
        }

        public interface ITestService
        {
            event EventHandler<CalculateResponse> CalculateResponseEvent;

            event EventHandler<CalculateResponseEventArgs> CalculateResponseEventWithArgs;

            String StringProperty { get; set; }

            int Int32Property { get; set; }

            DateTime DateProperty { get; set; }

            CalculateRequest CalculateRequestProperty { get; set; }

            CalculateResponse Calculate(CalculateRequest request);

            Task<CalculateResponse> CalculateAsync(CalculateRequest request);

            void CalculateVoid(CalculateRequest request);

            Task CalculateVoidAsync(CalculateRequest request);

            void CalculateVoidThrows(CalculateRequest request);

            CalculateResponse CalculateThrows(CalculateRequest request);

            String GetStringValue(String value);

            String GetStringNoInput();

            Task<String> GetStringNoInputAsync();

            void VoidNoInput();

            int GetInt32Value(int value);

            double CalculateMultiParameter(double a, double b);

            Task<double> CalculateMultiParameterAsync(double a, double b);

            void CalculateMultiParameterVoid(double a, double b);

            [ResonanceRpc(Timeout = 10)]
            CalculateResponse CalculateWithAttributeTimeout(CalculateRequest request);


            [ResonanceRpc(Timeout = 1)]
            CalculateResponse CalculateWithAttributeShortTimeout(CalculateRequest request);
        }

        public class TestService : ITestService
        {
            public static int InstanceCount { get; set; }

            public TestService()
            {
                InstanceCount++;
            }

            public event EventHandler<CalculateResponse> CalculateResponseEvent;
            public event EventHandler<CalculateResponseEventArgs> CalculateResponseEventWithArgs;

            private String _stringProperty;
            public String StringProperty
            {
                get
                {
                    return _stringProperty;
                }
                set
                {
                    _stringProperty = value;
                }
            }

            public int Int32Property { get; set; }

            public DateTime DateProperty { get; set; }
            public CalculateRequest CalculateRequestProperty { get; set; }

            public CalculateResponse Calculate(CalculateRequest request)
            {
                return new CalculateResponse() { Sum = request.A + request.B };
            }

            public Task<CalculateResponse> CalculateAsync(CalculateRequest request)
            {
                return Task.FromResult(new CalculateResponse() { Sum = request.A + request.B });
            }

            public CalculateResponse CalculateThrows(CalculateRequest request)
            {
                throw new Exception("Test Error");
            }

            public void CalculateVoid(CalculateRequest request)
            {
                receivedRequest = request;
            }

            public Task CalculateVoidAsync(CalculateRequest request)
            {
                receivedRequest = request;
                return Task.FromResult(true);
            }

            public void CalculateVoidThrows(CalculateRequest request)
            {
                throw new Exception("Test Error");
            }

            public string GetStringValue(string value)
            {
                return value;
            }

            public int GetInt32Value(int value)
            {
                return value;
            }

            public void RaiseCalculateResponseEvent(CalculateResponse response)
            {
                CalculateResponseEvent?.Invoke(this, response);
            }

            public void RaiseCalculateResponseEventWithArgs(CalculateResponse response)
            {
                CalculateResponseEventWithArgs?.Invoke(this, new CalculateResponseEventArgs() { Response = response });
            }

            public string GetStringNoInput()
            {
                return "Test";
            }

            public Task<string> GetStringNoInputAsync()
            {
                return Task.FromResult("Test");
            }

            public void VoidNoInput()
            {
                receivedRequest = new CalculateRequest() { A = 100 };
            }

            public double CalculateMultiParameter(double a, double b)
            {
                return a + b;
            }

            public Task<double> CalculateMultiParameterAsync(double a, double b)
            {
                return Task.FromResult(a + b);
            }

            public void CalculateMultiParameterVoid(double a, double b)
            {
                receivedRequest = new CalculateRequest() { A = a, B = b };
            }

            public CalculateResponse CalculateWithAttributeTimeout(CalculateRequest request)
            {
                Thread.Sleep(6000);
                return new CalculateResponse();
            }

            public CalculateResponse CalculateWithAttributeShortTimeout(CalculateRequest request)
            {
                Thread.Sleep(2000);
                return new CalculateResponse();
            }
        }

        [TestMethod]
        public void Service_Handles_Request_Method()
        {
            Reset();

            ResonanceTransporter t1 = new ResonanceTransporter(new InMemoryAdapter("TST"));
            ResonanceTransporter t2 = new ResonanceTransporter(new InMemoryAdapter("TST"));

            t1.Connect();
            t2.Connect();

            var testService = new TestService();

            t2.RegisterService<ITestService, TestService>(testService);

            var proxy = t1.CreateClientProxy<ITestService>();

            var request = new CalculateRequest() { A = 10, B = 5 };

            var response = proxy.Calculate(request);

            Assert.AreEqual(response.Sum, request.A + request.B);

            t2.UnregisterService<ITestService>();

            Assert.ThrowsException<ResonanceResponseException>(() =>
            {
                proxy.Calculate(request);
            });

            t1.Dispose(true);
            t2.Dispose(true);
        }

        [TestMethod]
        public void Singleton_Service_1()
        {
            Reset();

            ResonanceTransporter t1 = new ResonanceTransporter(new InMemoryAdapter("TST"));
            ResonanceTransporter t2 = new ResonanceTransporter(new InMemoryAdapter("TST"));

            t1.Connect();
            t2.Connect();

            var testService = new TestService();

            t2.RegisterService<ITestService, TestService>(testService);

            var proxy = t1.CreateClientProxy<ITestService>();

            var request = new CalculateRequest() { A = 10, B = 5 };

            var response = proxy.Calculate(request);

            Assert.AreEqual(response.Sum, request.A + request.B);

            t1.Dispose(true);
            t2.Dispose(true);
        }

        [TestMethod]
        public void Singleton_Service_2()
        {
            Reset();

            ResonanceTransporter t1 = new ResonanceTransporter(new InMemoryAdapter("TST"));
            ResonanceTransporter t2 = new ResonanceTransporter(new InMemoryAdapter("TST"));

            t1.Connect();
            t2.Connect();

            t2.RegisterService<ITestService, TestService>(RpcServiceCreationType.Singleton);

            var proxy = t1.CreateClientProxy<ITestService>();

            var request = new CalculateRequest() { A = 10, B = 5 };

            var response = proxy.Calculate(request);
            response = proxy.Calculate(request);

            Assert.IsTrue(TestService.InstanceCount == 1);

            t1.Dispose(true);
            t2.Dispose(true);
        }

        [TestMethod]
        public void Singleton_Service_3()
        {
            Reset();

            ResonanceTransporter t1 = new ResonanceTransporter(new InMemoryAdapter("TST"));
            ResonanceTransporter t2 = new ResonanceTransporter(new InMemoryAdapter("TST"));

            t1.Connect();
            t2.Connect();

            bool funcExecuted = false;

            t2.RegisterService<ITestService, TestService>(RpcServiceCreationType.Singleton, () =>
             {
                 funcExecuted = true;
                 return new TestService();
             });

            var proxy = t1.CreateClientProxy<ITestService>();

            var request = new CalculateRequest() { A = 10, B = 5 };

            var response = proxy.Calculate(request);
            response = proxy.Calculate(request);

            Assert.IsTrue(funcExecuted);
            Assert.IsTrue(TestService.InstanceCount == 1);

            t1.Dispose(true);
            t2.Dispose(true);
        }

        [TestMethod]
        public void Transient_Service_1()
        {
            Reset();

            ResonanceTransporter t1 = new ResonanceTransporter(new InMemoryAdapter("TST"));
            ResonanceTransporter t2 = new ResonanceTransporter(new InMemoryAdapter("TST"));

            t1.Connect();
            t2.Connect();

            t2.RegisterService<ITestService, TestService>(RpcServiceCreationType.Transient);

            var proxy = t1.CreateClientProxy<ITestService>();

            var request = new CalculateRequest() { A = 10, B = 5 };

            var response = proxy.Calculate(request);
            response = proxy.Calculate(request);

            Assert.IsTrue(TestService.InstanceCount == 2);

            t1.Dispose(true);
            t2.Dispose(true);
        }

        [TestMethod]
        public void Transient_Service_2()
        {
            Reset();

            ResonanceTransporter t1 = new ResonanceTransporter(new InMemoryAdapter("TST"));
            ResonanceTransporter t2 = new ResonanceTransporter(new InMemoryAdapter("TST"));

            t1.Connect();
            t2.Connect();

            t2.RegisterService<ITestService, TestService>(RpcServiceCreationType.Transient);

            var proxy = t1.CreateClientProxy<ITestService>();

            var request = new CalculateRequest() { A = 10, B = 5 };

            var response = proxy.Calculate(request);
            response = proxy.Calculate(request);

            Assert.IsTrue(TestService.InstanceCount == 2);

            t1.Dispose(true);
            t2.Dispose(true);
        }

        [TestMethod]
        public void Transient_Service_3()
        {
            Reset();

            ResonanceTransporter t1 = new ResonanceTransporter(new InMemoryAdapter("TST"));
            ResonanceTransporter t2 = new ResonanceTransporter(new InMemoryAdapter("TST"));

            t1.Connect();
            t2.Connect();

            bool funcExecuted = false;

            t2.RegisterService<ITestService, TestService>(RpcServiceCreationType.Transient, () =>
            {
                funcExecuted = true;
                return new TestService();
            });

            var proxy = t1.CreateClientProxy<ITestService>();

            var request = new CalculateRequest() { A = 10, B = 5 };

            var response = proxy.Calculate(request);
            response = proxy.Calculate(request);

            Assert.IsTrue(funcExecuted);
            Assert.IsTrue(TestService.InstanceCount == 2);

            t1.Dispose(true);
            t2.Dispose(true);
        }

        [TestMethod]
        public void Service_Handles_Request_Async_Method()
        {
            Reset();

            ResonanceTransporter t1 = new ResonanceTransporter(new InMemoryAdapter("TST"));
            ResonanceTransporter t2 = new ResonanceTransporter(new InMemoryAdapter("TST"));

            t1.Connect();
            t2.Connect();

            var testService = new TestService();

            t2.RegisterService<ITestService, TestService>(testService);

            var proxy = t1.CreateClientProxy<ITestService>();

            var request = new CalculateRequest() { A = 10, B = 5 };

            var response = proxy.CalculateAsync(request).GetAwaiter().GetResult();

            Assert.AreEqual(response.Sum, request.A + request.B);

            t2.UnregisterService<ITestService>();

            Assert.ThrowsException<ResonanceResponseException>(() =>
            {
                proxy.CalculateAsync(request).GetAwaiter().GetResult();
            });

            t1.Dispose(true);
            t2.Dispose(true);
        }

        [TestMethod]
        public void Service_Handles_Message_Method()
        {
            Reset();

            ResonanceTransporter t1 = new ResonanceTransporter(new InMemoryAdapter("TST"));
            ResonanceTransporter t2 = new ResonanceTransporter(new InMemoryAdapter("TST"));

            t1.MessageAcknowledgmentBehavior = ResonanceMessageAckBehavior.ReportErrors;
            t2.MessageAcknowledgmentBehavior = ResonanceMessageAckBehavior.ReportErrors;

            t1.Connect();
            t2.Connect();

            var testService = new TestService();

            t2.RegisterService<ITestService, TestService>(testService);

            var proxy = t1.CreateClientProxy<ITestService>();

            var request = new CalculateRequest() { A = 10, B = 5 };

            proxy.CalculateVoid(request);

            Assert.AreEqual(receivedRequest.A + receivedRequest.B, request.A + request.B);

            t2.UnregisterService<ITestService>();

            Assert.ThrowsException<ResonanceResponseException>(() =>
            {
                proxy.CalculateVoid(request);
            });

            t1.Dispose(true);
            t2.Dispose(true);
        }

        [TestMethod]
        public void Service_Handles_No_Input_Method()
        {
            Reset();

            ResonanceTransporter t1 = new ResonanceTransporter(new InMemoryAdapter("TST"));
            ResonanceTransporter t2 = new ResonanceTransporter(new InMemoryAdapter("TST"));

            t1.MessageAcknowledgmentBehavior = ResonanceMessageAckBehavior.ReportErrors;
            t2.MessageAcknowledgmentBehavior = ResonanceMessageAckBehavior.ReportErrors;

            t1.Connect();
            t2.Connect();

            var testService = new TestService();

            t2.RegisterService<ITestService, TestService>(testService);

            var proxy = t1.CreateClientProxy<ITestService>();

            var request = new CalculateRequest() { A = 10, B = 5 };

            String s = proxy.GetStringNoInput();

            Assert.AreEqual(s, "Test");

            t1.Dispose(true);
            t2.Dispose(true);
        }

        [TestMethod]
        public void Service_Handles_No_Input_Method_Async()
        {
            Reset();

            ResonanceTransporter t1 = new ResonanceTransporter(new InMemoryAdapter("TST"));
            ResonanceTransporter t2 = new ResonanceTransporter(new InMemoryAdapter("TST"));

            t1.MessageAcknowledgmentBehavior = ResonanceMessageAckBehavior.ReportErrors;
            t2.MessageAcknowledgmentBehavior = ResonanceMessageAckBehavior.ReportErrors;

            t1.Connect();
            t2.Connect();

            var testService = new TestService();

            t2.RegisterService<ITestService, TestService>(testService);

            var proxy = t1.CreateClientProxy<ITestService>();

            var request = new CalculateRequest() { A = 10, B = 5 };

            String s = proxy.GetStringNoInputAsync().GetAwaiter().GetResult();

            Assert.AreEqual(s, "Test");

            t1.Dispose(true);
            t2.Dispose(true);
        }

        [TestMethod]
        public void Service_Handles_Void_No_Input_Method()
        {
            Reset();

            ResonanceTransporter t1 = new ResonanceTransporter(new InMemoryAdapter("TST"));
            ResonanceTransporter t2 = new ResonanceTransporter(new InMemoryAdapter("TST"));

            t1.MessageAcknowledgmentBehavior = ResonanceMessageAckBehavior.ReportErrors;
            t2.MessageAcknowledgmentBehavior = ResonanceMessageAckBehavior.ReportErrors;

            t1.Connect();
            t2.Connect();

            var testService = new TestService();

            t2.RegisterService<ITestService, TestService>(testService);

            var proxy = t1.CreateClientProxy<ITestService>();

            proxy.VoidNoInput();

            Assert.AreEqual(receivedRequest.A, 100);

            t1.Dispose(true);
            t2.Dispose(true);
        }

        [TestMethod]
        public void Service_Handles_Multi_Parameter_Method()
        {
            Reset();

            ResonanceTransporter t1 = new ResonanceTransporter(new InMemoryAdapter("TST"));
            ResonanceTransporter t2 = new ResonanceTransporter(new InMemoryAdapter("TST"));

            t1.MessageAcknowledgmentBehavior = ResonanceMessageAckBehavior.ReportErrors;
            t2.MessageAcknowledgmentBehavior = ResonanceMessageAckBehavior.ReportErrors;

            t1.Connect();
            t2.Connect();

            var testService = new TestService();

            t2.RegisterService<ITestService, TestService>(testService);

            var proxy = t1.CreateClientProxy<ITestService>();

            var result = proxy.CalculateMultiParameter(10, 5);

            Assert.AreEqual(result, 15);

            t1.Dispose(true);
            t2.Dispose(true);
        }

        [TestMethod]
        public void Service_Handles_Multi_Parameter_Async_Method()
        {
            Reset();

            ResonanceTransporter t1 = new ResonanceTransporter(new InMemoryAdapter("TST"));
            ResonanceTransporter t2 = new ResonanceTransporter(new InMemoryAdapter("TST"));

            t1.MessageAcknowledgmentBehavior = ResonanceMessageAckBehavior.ReportErrors;
            t2.MessageAcknowledgmentBehavior = ResonanceMessageAckBehavior.ReportErrors;

            t1.Connect();
            t2.Connect();

            var testService = new TestService();

            t2.RegisterService<ITestService, TestService>(testService);

            var proxy = t1.CreateClientProxy<ITestService>();

            var result = proxy.CalculateMultiParameterAsync(10, 5).GetAwaiter().GetResult();

            Assert.AreEqual(result, 15);

            t1.Dispose(true);
            t2.Dispose(true);
        }

        [TestMethod]
        public void Service_Handles_Multi_Parameter_Void_Method()
        {

            Reset();
            ResonanceTransporter t1 = new ResonanceTransporter(new InMemoryAdapter("TST"));
            ResonanceTransporter t2 = new ResonanceTransporter(new InMemoryAdapter("TST"));

            t1.MessageAcknowledgmentBehavior = ResonanceMessageAckBehavior.ReportErrors;
            t2.MessageAcknowledgmentBehavior = ResonanceMessageAckBehavior.ReportErrors;

            t1.Connect();
            t2.Connect();

            var testService = new TestService();

            t2.RegisterService<ITestService, TestService>(testService);

            var proxy = t1.CreateClientProxy<ITestService>();

            proxy.CalculateMultiParameterVoid(10, 5);

            Assert.AreEqual(receivedRequest.A, 10);

            t1.Dispose(true);
            t2.Dispose(true);
        }

        [TestMethod]
        public void Service_Handles_Request_Method_With_Rpc_Attribute_Timeout()
        {
            Reset();

            ResonanceTransporter t1 = new ResonanceTransporter(new InMemoryAdapter("TST"));
            ResonanceTransporter t2 = new ResonanceTransporter(new InMemoryAdapter("TST"));

            t1.Connect();
            t2.Connect();

            var testService = new TestService();

            t2.RegisterService<ITestService, TestService>(testService);

            var proxy = t1.CreateClientProxy<ITestService>();

            var request = new CalculateRequest() { A = 10, B = 5 };

            var response = proxy.CalculateWithAttributeTimeout(request); //The timeout is 10 seconds while the service method delay is 6 and default timeout is 5 or 2.

            t1.Dispose(true);
            t2.Dispose(true);
        }

        [TestMethod]
        public void Service_Handles_Request_Method_With_Rpc_Attribute_Timeout_Throws_Exception()
        {
            Reset();

            ResonanceTransporter t1 = new ResonanceTransporter(new InMemoryAdapter("TST"));
            ResonanceTransporter t2 = new ResonanceTransporter(new InMemoryAdapter("TST"));

            t1.Connect();
            t2.Connect();

            var testService = new TestService();

            t2.RegisterService<ITestService, TestService>(testService);

            var proxy = t1.CreateClientProxy<ITestService>();

            var request = new CalculateRequest() { A = 10, B = 5 };

            Assert.ThrowsException<TimeoutException>(() =>
            {
                var response = proxy.CalculateWithAttributeShortTimeout(request);
            });

            t1.Dispose(true);
            t2.Dispose(true);
        }

        [TestMethod]
        public void Service_Handles_Message_Async_Method()
        {
            Reset();

            ResonanceTransporter t1 = new ResonanceTransporter(new InMemoryAdapter("TST"));
            ResonanceTransporter t2 = new ResonanceTransporter(new InMemoryAdapter("TST"));

            t1.MessageAcknowledgmentBehavior = ResonanceMessageAckBehavior.ReportErrors;
            t2.MessageAcknowledgmentBehavior = ResonanceMessageAckBehavior.ReportErrors;

            t1.Connect();
            t2.Connect();

            var testService = new TestService();

            t2.RegisterService<ITestService, TestService>(testService);

            var proxy = t1.CreateClientProxy<ITestService>();

            var request = new CalculateRequest() { A = 10, B = 5 };

            proxy.CalculateVoidAsync(request).GetAwaiter().GetResult();

            Assert.AreEqual(receivedRequest.A + receivedRequest.B, request.A + request.B);

            t2.UnregisterService<ITestService>();

            Assert.ThrowsException<ResonanceResponseException>(() =>
            {
                proxy.CalculateVoidAsync(request).GetAwaiter().GetResult();
            });

            t1.Dispose(true);
            t2.Dispose(true);
        }

        [TestMethod]
        public void Service_Handles_Message_Method_Throws_Exception()
        {
            Reset();

            ResonanceTransporter t1 = new ResonanceTransporter(new InMemoryAdapter("TST"));
            ResonanceTransporter t2 = new ResonanceTransporter(new InMemoryAdapter("TST"));

            t1.MessageAcknowledgmentBehavior = ResonanceMessageAckBehavior.ReportErrors;
            t2.MessageAcknowledgmentBehavior = ResonanceMessageAckBehavior.ReportErrors;

            t1.Connect();
            t2.Connect();

            var testService = new TestService();

            t2.RegisterService<ITestService, TestService>(testService);

            var proxy = t1.CreateClientProxy<ITestService>();

            var request = new CalculateRequest() { A = 10, B = 5 };

            Assert.ThrowsException<ResonanceResponseException>(() =>
            {
                proxy.CalculateVoidThrows(request);
            }, "Test Error");

            t2.UnregisterService<ITestService>();

            t1.Dispose(true);
            t2.Dispose(true);
        }

        [TestMethod]
        public void Service_Handles_Request_Method_Throws_Exception()
        {
            Reset();

            ResonanceTransporter t1 = new ResonanceTransporter(new InMemoryAdapter("TST"));
            ResonanceTransporter t2 = new ResonanceTransporter(new InMemoryAdapter("TST"));

            t1.Connect();
            t2.Connect();

            var testService = new TestService();

            t2.RegisterService<ITestService, TestService>(testService);

            var proxy = t1.CreateClientProxy<ITestService>();

            var request = new CalculateRequest() { A = 10, B = 5 };

            Assert.ThrowsException<ResonanceResponseException>(() =>
            {
                proxy.CalculateThrows(request);
            }, "Test Error");

            t2.UnregisterService<ITestService>();

            t1.Dispose(true);
            t2.Dispose(true);
        }

        [TestMethod]
        public void Service_Handles_Message_Method_Primitive_String()
        {
            Reset();

            ResonanceTransporter t1 = new ResonanceTransporter(new InMemoryAdapter("TST"));
            ResonanceTransporter t2 = new ResonanceTransporter(new InMemoryAdapter("TST"));

            t1.MessageAcknowledgmentBehavior = ResonanceMessageAckBehavior.ReportErrors;
            t2.MessageAcknowledgmentBehavior = ResonanceMessageAckBehavior.ReportErrors;

            t1.Connect();
            t2.Connect();

            var testService = new TestService();

            t2.RegisterService<ITestService, TestService>(testService);

            var proxy = t1.CreateClientProxy<ITestService>();

            String s = proxy.GetStringValue("Test");

            Assert.AreEqual(s, "Test");

            t2.UnregisterService<ITestService>();

            Assert.ThrowsException<ResonanceResponseException>(() =>
            {
                proxy.GetStringValue("Test");
            });

            t1.Dispose(true);
            t2.Dispose(true);
        }

        [TestMethod]
        public void Service_Handles_Message_Method_Primitive_Int32()
        {
            Reset();

            ResonanceTransporter t1 = new ResonanceTransporter(new InMemoryAdapter("TST"));
            ResonanceTransporter t2 = new ResonanceTransporter(new InMemoryAdapter("TST"));

            t1.MessageAcknowledgmentBehavior = ResonanceMessageAckBehavior.ReportErrors;
            t2.MessageAcknowledgmentBehavior = ResonanceMessageAckBehavior.ReportErrors;

            t1.Connect();
            t2.Connect();

            var testService = new TestService();

            t2.RegisterService<ITestService, TestService>(testService);

            var proxy = t1.CreateClientProxy<ITestService>();

            int s = proxy.GetInt32Value(10);

            Assert.AreEqual(s, 10);

            t2.UnregisterService<ITestService>();

            Assert.ThrowsException<ResonanceResponseException>(() =>
            {
                proxy.GetStringValue("Test");
            });

            t1.Dispose(true);
            t2.Dispose(true);
        }

        [TestMethod]
        public void Service_Handles_String_Property_Get_Set()
        {
            Reset();

            ResonanceTransporter t1 = new ResonanceTransporter(new InMemoryAdapter("TST"));
            ResonanceTransporter t2 = new ResonanceTransporter(new InMemoryAdapter("TST"));

            t1.MessageAcknowledgmentBehavior = ResonanceMessageAckBehavior.ReportErrors;
            t2.MessageAcknowledgmentBehavior = ResonanceMessageAckBehavior.ReportErrors;

            t1.Connect();
            t2.Connect();

            var testService = new TestService();

            t2.RegisterService<ITestService, TestService>(testService);

            var proxy = t1.CreateClientProxy<ITestService>();

            proxy.StringProperty = "Test";

            String value = proxy.StringProperty;

            Assert.AreEqual(value, "Test");

            t2.UnregisterService<ITestService>();

            Assert.ThrowsException<ResonanceResponseException>(() =>
            {
                proxy.StringProperty = "Test";
            });

            t1.Dispose(true);
            t2.Dispose(true);
        }

        [TestMethod]
        public void Service_Handles_Int32_Property_Get_Set()
        {
            Reset();

            ResonanceTransporter t1 = new ResonanceTransporter(new InMemoryAdapter("TST"));
            ResonanceTransporter t2 = new ResonanceTransporter(new InMemoryAdapter("TST"));

            t1.MessageAcknowledgmentBehavior = ResonanceMessageAckBehavior.ReportErrors;
            t2.MessageAcknowledgmentBehavior = ResonanceMessageAckBehavior.ReportErrors;

            t1.Connect();
            t2.Connect();

            var testService = new TestService();

            t2.RegisterService<ITestService, TestService>(testService);

            var proxy = t1.CreateClientProxy<ITestService>();

            proxy.Int32Property = 100;

            int value = proxy.Int32Property;

            Assert.AreEqual(value, 100);

            t1.Dispose(true);
            t2.Dispose(true);
        }

        [TestMethod]
        public void Service_Handles_DateTime_Property_Get_Set()
        {
            Reset();

            ResonanceTransporter t1 = new ResonanceTransporter(new InMemoryAdapter("TST"));
            ResonanceTransporter t2 = new ResonanceTransporter(new InMemoryAdapter("TST"));

            t1.MessageAcknowledgmentBehavior = ResonanceMessageAckBehavior.ReportErrors;
            t2.MessageAcknowledgmentBehavior = ResonanceMessageAckBehavior.ReportErrors;

            t1.Connect();
            t2.Connect();

            var testService = new TestService();

            t2.RegisterService<ITestService, TestService>(testService);

            var proxy = t1.CreateClientProxy<ITestService>();

            DateTime date = DateTime.Now;

            proxy.DateProperty = date;

            var value = proxy.DateProperty;

            Assert.AreEqual(value.ToString(), date.ToString());

            t1.Dispose(true);
            t2.Dispose(true);
        }

        [TestMethod]
        public void Service_Handles_Object_Property_Get_Set()
        {
            Reset();

            ResonanceTransporter t1 = new ResonanceTransporter(new InMemoryAdapter("TST"));
            ResonanceTransporter t2 = new ResonanceTransporter(new InMemoryAdapter("TST"));

            t1.MessageAcknowledgmentBehavior = ResonanceMessageAckBehavior.ReportErrors;
            t2.MessageAcknowledgmentBehavior = ResonanceMessageAckBehavior.ReportErrors;

            t1.Connect();
            t2.Connect();

            var testService = new TestService();

            t2.RegisterService<ITestService, TestService>(testService);

            var proxy = t1.CreateClientProxy<ITestService>();

            CalculateRequest request = new CalculateRequest() { A = 1, B = 2 };

            Assert.IsTrue(proxy.CalculateRequestProperty == null);

            proxy.CalculateRequestProperty = request;

            var value = proxy.CalculateRequestProperty;

            Assert.AreEqual(value.A + value.B, request.A + request.B);

            t1.Dispose(true);
            t2.Dispose(true);
        }

        [TestMethod]
        public void Service_Handles_Event()
        {
            Reset();

            ResonanceTransporter t1 = new ResonanceTransporter(new InMemoryAdapter("TST"));
            ResonanceTransporter t2 = new ResonanceTransporter(new InMemoryAdapter("TST"));

            t1.MessageAcknowledgmentBehavior = ResonanceMessageAckBehavior.ReportErrors;
            t2.MessageAcknowledgmentBehavior = ResonanceMessageAckBehavior.ReportErrors;

            t1.Connect();
            t2.Connect();

            var testService = new TestService();

            t2.RegisterService<ITestService, TestService>(testService);

            var proxy = t1.CreateClientProxy<ITestService>();

            List<double> sums = new List<double>();

            proxy.CalculateResponseEvent += (x, response) =>
            {
                sums.Add(response.Sum);
            };

            Thread.Sleep(1000);

            testService.RaiseCalculateResponseEvent(new CalculateResponse() { Sum = 1 });
            testService.RaiseCalculateResponseEvent(new CalculateResponse() { Sum = 2 });
            testService.RaiseCalculateResponseEvent(new CalculateResponse() { Sum = 3 });

            TestHelper.WaitWhile(() => sums.Count < 3, TimeSpan.FromSeconds(5));

            Assert.AreEqual(sums[0], 1);
            Assert.AreEqual(sums[1], 2);
            Assert.AreEqual(sums[2], 3);

            t2.UnregisterService<ITestService>();

            t1.Dispose(true);
            t2.Dispose(true);
        }

        [TestMethod]
        public void Service_Handles_Event_Late_Bound()
        {
            Reset();

            ResonanceTransporter t1 = new ResonanceTransporter(new InMemoryAdapter("TST"));
            ResonanceTransporter t2 = new ResonanceTransporter(new InMemoryAdapter("TST"));

            t1.MessageAcknowledgmentBehavior = ResonanceMessageAckBehavior.ReportErrors;
            t2.MessageAcknowledgmentBehavior = ResonanceMessageAckBehavior.ReportErrors;

            var testService = new TestService();

            t2.RegisterService<ITestService, TestService>(testService);

            var proxy = t1.CreateClientProxy<ITestService>();

            List<double> sums = new List<double>();

            proxy.CalculateResponseEvent += (x, response) =>
            {
                sums.Add(response.Sum);
            };

            //Connect after event registration...
            t2.Connect(); //Connect the event receiver first because event registration happens right after connection...
            t1.Connect();

            Thread.Sleep(1000);

            testService.RaiseCalculateResponseEvent(new CalculateResponse() { Sum = 1 });
            testService.RaiseCalculateResponseEvent(new CalculateResponse() { Sum = 2 });
            testService.RaiseCalculateResponseEvent(new CalculateResponse() { Sum = 3 });

            TestHelper.WaitWhile(() => sums.Count < 3, TimeSpan.FromSeconds(5));

            Assert.AreEqual(sums[0], 1);
            Assert.AreEqual(sums[1], 2);
            Assert.AreEqual(sums[2], 3);

            t2.UnregisterService<ITestService>();

            t1.Dispose(true);
            t2.Dispose(true);
        }

        [TestMethod]
        public void Service_Handles_Event_With_EventArgs()
        {
            Reset();

            ResonanceTransporter t1 = new ResonanceTransporter(new InMemoryAdapter("TST"));
            ResonanceTransporter t2 = new ResonanceTransporter(new InMemoryAdapter("TST"));

            t1.MessageAcknowledgmentBehavior = ResonanceMessageAckBehavior.ReportErrors;
            t2.MessageAcknowledgmentBehavior = ResonanceMessageAckBehavior.ReportErrors;

            t1.Connect();
            t2.Connect();

            var testService = new TestService();

            t2.RegisterService<ITestService, TestService>(testService);

            var proxy = t1.CreateClientProxy<ITestService>();

            List<double> sums = new List<double>();

            proxy.CalculateResponseEventWithArgs += (x, e) =>
            {
                sums.Add(e.Response.Sum);
            };

            Thread.Sleep(1000);

            testService.RaiseCalculateResponseEventWithArgs(new CalculateResponse() { Sum = 1 });
            testService.RaiseCalculateResponseEventWithArgs(new CalculateResponse() { Sum = 2 });
            testService.RaiseCalculateResponseEventWithArgs(new CalculateResponse() { Sum = 3 });

            TestHelper.WaitWhile(() => sums.Count < 3, TimeSpan.FromSeconds(5));

            Assert.AreEqual(sums[0], 1);
            Assert.AreEqual(sums[1], 2);
            Assert.AreEqual(sums[2], 3);

            t2.UnregisterService<ITestService>();

            t1.Dispose(true);
            t2.Dispose(true);
        }
    }
}