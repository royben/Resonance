using Microsoft.VisualStudio.TestTools.UnitTesting;
using Resonance.Adapters.InMemory;
using Resonance.Tests.Common;
using Resonance.Messages;

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
        private static CalculateRequest _receivedRequest;
        private static IResonanceTransporter _receivedTransporter;

        public class Handlers
        {
            internal void HandleMessageNoTransporter(ResonanceMessage<CalculateRequest> message)
            {
                _receivedRequest = message.Object;
            }

            internal void HandleMessageWithTransporter(IResonanceTransporter transporter, ResonanceMessage<CalculateRequest> message)
            {
                _receivedRequest = message.Object;
                _receivedTransporter = transporter;
            }

            internal Task HandleMessageAsyncNoTransporter(ResonanceMessage<CalculateRequest> message)
            {
                _receivedRequest = message.Object;
                return Task.FromResult(true);
            }

            internal Task HandleMessageAsyncWithTransporter(IResonanceTransporter transporter, ResonanceMessage<CalculateRequest> message)
            {
                _receivedRequest = message.Object;
                _receivedTransporter = transporter;
                return Task.FromResult(true);
            }

            internal void HandleRequestNoResponse(IResonanceTransporter transporter, ResonanceMessage<CalculateRequest> message)
            {
                transporter.SendResponse(new CalculateResponse() { Sum = message.Object.A + message.Object.B }, message.Token);
            }

            internal async Task HandleRequestAsyncNoResponse(IResonanceTransporter transporter, ResonanceMessage<CalculateRequest> message)
            {
                await Task.Delay(100);
                transporter.SendResponse(new CalculateResponse() { Sum = message.Object.A + message.Object.B }, message.Token);
            }

            internal ResonanceActionResult<CalculateResponse> HandleRequestWithResponse(CalculateRequest request)
            {
                return new CalculateResponse() { Sum = request.A + request.B };
            }

            internal ResonanceActionResult<CalculateResponse> HandleRequestWithResponseWithTransporter(IResonanceTransporter transporter, CalculateRequest request)
            {
                _receivedTransporter = transporter;
                return new CalculateResponse() { Sum = request.A + request.B };
            }

            internal async Task<ResonanceActionResult<CalculateResponse>> HandleRequestAsyncWithResponse(CalculateRequest request)
            {
                await Task.Delay(100);
                return new CalculateResponse() { Sum = request.A + request.B };
            }

            internal async Task<ResonanceActionResult<CalculateResponse>> HandleRequestAsyncWithResponseWithTransporter(IResonanceTransporter transporter, CalculateRequest request)
            {
                _receivedTransporter = transporter;
                await Task.Delay(100);
                return new CalculateResponse() { Sum = request.A + request.B };
            }
        }

        public override void Init()
        {
            base.Init();
            _receivedRequest = null;
        }

        [TestMethod]
        public void Message_Handler_No_Transporter()
        {
            ResonanceTransporter t1 = new ResonanceTransporter(new InMemoryAdapter("TST"));
            ResonanceTransporter t2 = new ResonanceTransporter(new InMemoryAdapter("TST"));

            t1.Connect();
            t2.Connect();

            Handlers handlers = new Handlers();

            IDisposable handler = t2.CreateHandlerBuilder().ForMessage<CalculateRequest>().Build(handlers.HandleMessageNoTransporter);

            var request = new CalculateRequest() { A = 10, B = 15 };

            t1.Send(request);

            TestHelper.WaitWhile(() => _receivedRequest == null, TimeSpan.FromSeconds(5));

            Assert.AreEqual(_receivedRequest.A + _receivedRequest.B, request.A + request.B);

            _receivedRequest = null;

            handler.Dispose();

            t1.Send(request);

            Thread.Sleep(500);

            Assert.IsNull(_receivedRequest);

            t1.Dispose(true);
            t2.Dispose(true);
        }

        [TestMethod]
        public void Message_Handler_With_Transporter()
        {
            ResonanceTransporter t1 = new ResonanceTransporter(new InMemoryAdapter("TST"));
            ResonanceTransporter t2 = new ResonanceTransporter(new InMemoryAdapter("TST"));

            t1.Connect();
            t2.Connect();

            Handlers handlers = new Handlers();

            IDisposable handler = t2.CreateHandlerBuilder()
                .ForMessage<CalculateRequest>()
                .IncludeTransporter()
                .Build(handlers.HandleMessageWithTransporter);

            var request = new CalculateRequest() { A = 10, B = 15 };

            t1.Send(request);

            TestHelper.WaitWhile(() => _receivedRequest == null, TimeSpan.FromSeconds(5));

            Assert.AreEqual(_receivedRequest.A + _receivedRequest.B, request.A + request.B);
            Assert.AreEqual(_receivedTransporter, t2);

            _receivedRequest = null;

            handler.Dispose();

            t1.Send(request);

            Thread.Sleep(500);

            Assert.IsNull(_receivedRequest);

            t1.Dispose(true);
            t2.Dispose(true);
        }

        [TestMethod]
        public void Message_Handler_Async_No_Transporter()
        {
            ResonanceTransporter t1 = new ResonanceTransporter(new InMemoryAdapter("TST"));
            ResonanceTransporter t2 = new ResonanceTransporter(new InMemoryAdapter("TST"));

            t1.Connect();
            t2.Connect();

            Handlers handlers = new Handlers();

            IDisposable handler = t2.CreateHandlerBuilder().ForMessage<CalculateRequest>().IsAsync().Build(handlers.HandleMessageAsyncNoTransporter);

            var request = new CalculateRequest() { A = 10, B = 15 };

            t1.Send(request);

            TestHelper.WaitWhile(() => _receivedRequest == null, TimeSpan.FromSeconds(5));

            Assert.AreEqual(_receivedRequest.A + _receivedRequest.B, request.A + request.B);

            _receivedRequest = null;

            handler.Dispose();

            t1.Send(request);

            Thread.Sleep(500);

            Assert.IsNull(_receivedRequest);

            t1.Dispose(true);
            t2.Dispose(true);
        }

        [TestMethod]
        public void Message_Handler_Async_With_Transporter()
        {
            ResonanceTransporter t1 = new ResonanceTransporter(new InMemoryAdapter("TST"));
            ResonanceTransporter t2 = new ResonanceTransporter(new InMemoryAdapter("TST"));

            t1.Connect();
            t2.Connect();

            Handlers handlers = new Handlers();

            IDisposable handler = t2.CreateHandlerBuilder()
                .ForMessage<CalculateRequest>()
                .IncludeTransporter()
                .IsAsync()
                .Build(handlers.HandleMessageAsyncWithTransporter);

            var request = new CalculateRequest() { A = 10, B = 15 };

            t1.Send(request);

            TestHelper.WaitWhile(() => _receivedRequest == null, TimeSpan.FromSeconds(5));

            Assert.AreEqual(_receivedRequest.A + _receivedRequest.B, request.A + request.B);
            Assert.AreEqual(_receivedTransporter, t2);

            _receivedRequest = null;

            handler.Dispose();

            t1.Send(request);

            Thread.Sleep(500);

            Assert.IsNull(_receivedRequest);

            t1.Dispose(true);
            t2.Dispose(true);
        }

        [TestMethod]
        public void Request_Handler_No_Response()
        {
            ResonanceTransporter t1 = new ResonanceTransporter(new InMemoryAdapter("TST"));
            ResonanceTransporter t2 = new ResonanceTransporter(new InMemoryAdapter("TST"));

            t1.Connect();
            t2.Connect();

            Handlers handlers = new Handlers();

            IDisposable handler = t2.CreateHandlerBuilder()
                .ForRequest<CalculateRequest>()
                .Build(handlers.HandleRequestNoResponse);

            var request = new CalculateRequest() { A = 10, B = 15 };

            var response = t1.SendRequest<CalculateRequest, CalculateResponse>(request);

            Assert.AreEqual(response.Sum, request.A + request.B);

            handler.Dispose();

            Assert.ThrowsException<TimeoutException>(() =>
            {
                t1.SendRequest<CalculateRequest, CalculateResponse>(request,new ResonanceRequestConfig() { Timeout = TimeSpan.FromSeconds(1) });
            });

            t1.Dispose(true);
            t2.Dispose(true);
        }

        [TestMethod]
        public void Request_Handler_Async_No_Response()
        {
            ResonanceTransporter t1 = new ResonanceTransporter(new InMemoryAdapter("TST"));
            ResonanceTransporter t2 = new ResonanceTransporter(new InMemoryAdapter("TST"));

            t1.Connect();
            t2.Connect();

            Handlers handlers = new Handlers();

            IDisposable handler = t2.CreateHandlerBuilder()
                .ForRequest<CalculateRequest>()
                .IsAsync()
                .Build(handlers.HandleRequestAsyncNoResponse);

            var request = new CalculateRequest() { A = 10, B = 15 };

            var response = t1.SendRequest<CalculateRequest, CalculateResponse>(request);

            Assert.AreEqual(response.Sum, request.A + request.B);

            handler.Dispose();

            Assert.ThrowsException<TimeoutException>(() =>
            {
                t1.SendRequest<CalculateRequest, CalculateResponse>(request, new ResonanceRequestConfig() { Timeout = TimeSpan.FromSeconds(1) });
            });

            t1.Dispose(true);
            t2.Dispose(true);
        }

        [TestMethod]
        public void Request_Handler_With_Response()
        {
            ResonanceTransporter t1 = new ResonanceTransporter(new InMemoryAdapter("TST"));
            ResonanceTransporter t2 = new ResonanceTransporter(new InMemoryAdapter("TST"));

            t1.Connect();
            t2.Connect();

            Handlers handlers = new Handlers();

            IDisposable handler = t2.CreateHandlerBuilder()
                .ForRequest<CalculateRequest>()
                .WithResponse<CalculateResponse>()
                .Build(handlers.HandleRequestWithResponse);

            var request = new CalculateRequest() { A = 10, B = 15 };

            var response = t1.SendRequest<CalculateRequest, CalculateResponse>(request);

            Assert.AreEqual(response.Sum, request.A + request.B);

            handler.Dispose();

            Assert.ThrowsException<TimeoutException>(() =>
            {
                t1.SendRequest<CalculateRequest, CalculateResponse>(request, new ResonanceRequestConfig() { Timeout = TimeSpan.FromSeconds(1) });
            });

            t1.Dispose(true);
            t2.Dispose(true);
        }

        [TestMethod]
        public void Request_Handler_With_Response_With_Transporter()
        {
            ResonanceTransporter t1 = new ResonanceTransporter(new InMemoryAdapter("TST"));
            ResonanceTransporter t2 = new ResonanceTransporter(new InMemoryAdapter("TST"));

            t1.Connect();
            t2.Connect();

            Handlers handlers = new Handlers();

            IDisposable handler = t2.CreateHandlerBuilder()
                .ForRequest<CalculateRequest>()
                .WithResponse<CalculateResponse>()
                .IncludeTransporter()
                .Build(handlers.HandleRequestWithResponseWithTransporter);

            var request = new CalculateRequest() { A = 10, B = 15 };

            var response = t1.SendRequest<CalculateRequest, CalculateResponse>(request);

            Assert.AreEqual(response.Sum, request.A + request.B);
            Assert.AreEqual(_receivedTransporter, t2);

            handler.Dispose();

            Assert.ThrowsException<TimeoutException>(() =>
            {
                t1.SendRequest<CalculateRequest, CalculateResponse>(request, new ResonanceRequestConfig() { Timeout = TimeSpan.FromSeconds(1) });
            });

            t1.Dispose(true);
            t2.Dispose(true);
        }

        [TestMethod]
        public void Request_Handler_Async_With_Response()
        {
            ResonanceTransporter t1 = new ResonanceTransporter(new InMemoryAdapter("TST"));
            ResonanceTransporter t2 = new ResonanceTransporter(new InMemoryAdapter("TST"));

            t1.Connect();
            t2.Connect();

            Handlers handlers = new Handlers();

            IDisposable handler = t2.CreateHandlerBuilder()
                .ForRequest<CalculateRequest>()
                .WithResponse<CalculateResponse>()
                .IsAsync()
                .Build(handlers.HandleRequestAsyncWithResponse);

            var request = new CalculateRequest() { A = 10, B = 15 };

            var response = t1.SendRequest<CalculateRequest, CalculateResponse>(request);

            Assert.AreEqual(response.Sum, request.A + request.B);

            handler.Dispose();

            Assert.ThrowsException<TimeoutException>(() =>
            {
                t1.SendRequest<CalculateRequest, CalculateResponse>(request, new ResonanceRequestConfig() { Timeout = TimeSpan.FromSeconds(1) });
            });

            t1.Dispose(true);
            t2.Dispose(true);
        }

        [TestMethod]
        public void Request_Handler_Async_With_Response_With_Transporter()
        {
            ResonanceTransporter t1 = new ResonanceTransporter(new InMemoryAdapter("TST"));
            ResonanceTransporter t2 = new ResonanceTransporter(new InMemoryAdapter("TST"));

            t1.Connect();
            t2.Connect();

            Handlers handlers = new Handlers();

            IDisposable handler = t2.CreateHandlerBuilder()
                .ForRequest<CalculateRequest>()
                .WithResponse<CalculateResponse>()
                .IncludeTransporter()
                .IsAsync()
                .Build(handlers.HandleRequestAsyncWithResponseWithTransporter);

            var request = new CalculateRequest() { A = 10, B = 15 };

            var response = t1.SendRequest<CalculateRequest, CalculateResponse>(request);

            Assert.AreEqual(response.Sum, request.A + request.B);
            Assert.AreEqual(_receivedTransporter, t2);

            handler.Dispose();

            Assert.ThrowsException<TimeoutException>(() =>
            {
                t1.SendRequest<CalculateRequest, CalculateResponse>(request, new ResonanceRequestConfig() { Timeout = TimeSpan.FromSeconds(1) });
            });

            t1.Dispose(true);
            t2.Dispose(true);
        }

        [TestMethod]
        public void Message_Handler()
        {
            ResonanceTransporter t1 = new ResonanceTransporter(new InMemoryAdapter("TST"));
            ResonanceTransporter t2 = new ResonanceTransporter(new InMemoryAdapter("TST"));

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
            ResonanceTransporter t1 = new ResonanceTransporter(new InMemoryAdapter("TST"));
            ResonanceTransporter t2 = new ResonanceTransporter(new InMemoryAdapter("TST"));

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
            ResonanceTransporter t1 = new ResonanceTransporter(new InMemoryAdapter("TST"));
            ResonanceTransporter t2 = new ResonanceTransporter(new InMemoryAdapter("TST"));

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


            ResonanceTransporter t1 = new ResonanceTransporter(new InMemoryAdapter("TST"));
            ResonanceTransporter t2 = new ResonanceTransporter(new InMemoryAdapter("TST"));

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


            ResonanceTransporter t1 = new ResonanceTransporter(new InMemoryAdapter("TST"));
            ResonanceTransporter t2 = new ResonanceTransporter(new InMemoryAdapter("TST"));

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
