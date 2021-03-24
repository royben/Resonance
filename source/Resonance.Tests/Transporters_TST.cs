using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Resonance.Adapters;
using Resonance.Adapters.InMemory;
using Resonance.Exceptions;
using Resonance.Tests.Common;
using Resonance.Tests.Common.Logging;
using Resonance.Tests.Common.Messages;
using Resonance.Tests.Common.Transcoding;
using Resonance.Transporters;

namespace Resonance.Tests
{
    [TestClass]
    [TestCategory("Transporters")]
    public class Transporters_TST : ResonanceTest
    {
        [TestMethod]
        public void Send_And_Receive_Standard_Request()
        {
            Init();

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

            Assert.AreEqual(response.Sum, request.A + request.B);
        }

        [TestMethod]
        public void Send_And_Receive_Standard_Request_With_Error()
        {
            Init();

            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            ResonanceJsonTransporter t2 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));

            t1.Connect().Wait();
            t2.Connect().Wait();

            t2.RequestReceived += (s, e) =>
            {
                CalculateRequest receivedRequest = e.Request.Message as CalculateRequest;
                t2.SendErrorResponse("Error Message", e.Request.Token);
            };

            Assert.ThrowsException<ResonanceResponseException>(() =>
            {
                var response = t1.SendRequest<CalculateRequest, CalculateResponse>(new CalculateRequest()).GetAwaiter().GetResult();
            }, "Error Message");

            t1.Dispose(true);
            t2.Dispose(true);
        }

        [TestMethod]
        public void Send_And_Receive_Continuous_Request()
        {
            Init();

            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            ResonanceJsonTransporter t2 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));

            t1.Connect().Wait();
            t2.Connect().Wait();

            t2.RequestReceived += (s, e) =>
            {
                ProgressRequest receivedRequest = e.Request.Message as ProgressRequest;

                Task.Factory.StartNew(async () =>
                {
                    for (int i = 0; i < receivedRequest.Count; i++)
                    {
                        await t2.SendResponse(new ProgressResponse() { Value = i }, e.Request.Token);
                        Thread.Sleep(receivedRequest.Interval);
                    }

                    await t2.SendResponse(new ProgressResponse() { Value = receivedRequest.Count }, e.Request.Token, new ResonanceResponseConfig()
                    {
                        Completed = true
                    });

                });
            };

            int currentValue = -1;
            bool isCompleted = false;

            var request = new ProgressRequest() { Count = 100, Interval = TimeSpan.FromMilliseconds(30) };
            var subscription = t1.SendContinuousRequest<ProgressRequest, ProgressResponse>(request).Subscribe((response) =>
            {
                //Response

                Assert.IsTrue(response.Value == currentValue + 1);

                currentValue = response.Value;
            }, (ex) =>
            {
                //Error
            }, () =>
            {
                //Completed
                isCompleted = true;
            });

            subscription.WaitAsync().Wait();

            Assert.AreEqual(currentValue, request.Count);
            Assert.IsTrue(isCompleted);

            t1.Dispose(true);
            t2.Dispose(true);
        }

        [TestMethod]
        public void Send_And_Receive_Standard_Request_With_Compression()
        {
            Init();

            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            ResonanceJsonTransporter t2 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));

            t1.Encoder.CompressionConfiguration.Enable = true;
            t2.Encoder.CompressionConfiguration.Enable = true;

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

            Assert.AreEqual(response.Sum, request.A + request.B);
        }

        [TestMethod]
        public void Send_And_Receive_Continuous_Request_With_Error()
        {
            Init();

            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            ResonanceJsonTransporter t2 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));

            t1.Connect().Wait();
            t2.Connect().Wait();

            t2.RequestReceived += (s, e) =>
            {
                ProgressRequest receivedRequest = e.Request.Message as ProgressRequest;

                Task.Factory.StartNew(async () =>
                {
                    for (int i = 0; i < receivedRequest.Count; i++)
                    {
                        await t2.SendResponse(new ProgressResponse() { Value = i }, e.Request.Token);
                        Thread.Sleep(receivedRequest.Interval);
                    }

                    await t2.SendErrorResponse("Test Exception", e.Request.Token);

                    Thread.Sleep(receivedRequest.Interval);

                    await t2.SendResponse(new ProgressResponse() { Value = receivedRequest.Count }, e.Request.Token, new ResonanceResponseConfig()
                    {
                        Completed = true
                    });

                });
            };

            int currentValue = -1;
            bool isCompleted = false;
            bool hasError = false;

            var request = new ProgressRequest() { Count = 100, Interval = TimeSpan.FromMilliseconds(30) };
            var subscription = t1.SendContinuousRequest<ProgressRequest, ProgressResponse>(request).Subscribe((response) =>
            {
                //Response

                Assert.IsTrue(response.Value == currentValue + 1);

                currentValue = response.Value;
            }, (ex) =>
            {
                //Error
                hasError = true;

                Assert.AreEqual(ex.Message, "Test Exception");
            }, () =>
            {
                //Completed
                isCompleted = true;
            });

            Assert.ThrowsException<ResonanceResponseException>(() =>
            {
                subscription.WaitAsync().GetAwaiter().GetResult();
            }, "Test Exception");

            Assert.AreEqual(currentValue, request.Count - 1);
            Assert.IsFalse(isCompleted);
            Assert.IsTrue(hasError);

            t1.Dispose(true);
            t2.Dispose(true);
        }

        [TestMethod]
        public void Request_Timeout_Throws_Exception()
        {
            Init();

            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            ResonanceJsonTransporter t2 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));

            t1.Connect().Wait();
            t2.Connect().Wait();

            t2.RequestReceived += (s, e) =>
            {
                Thread.Sleep(1000);
                CalculateRequest receivedRequest = e.Request.Message as CalculateRequest;
                t2.SendResponse(new CalculateResponse() { Sum = receivedRequest.A + receivedRequest.B }, e.Request.Token);
            };

            var request = new CalculateRequest() { A = 10, B = 15 };

            Assert.ThrowsException<TimeoutException>(() =>
            {
                var response = t1.SendRequest<CalculateRequest, CalculateResponse>(request, new ResonanceRequestConfig()
                {
                    Timeout = TimeSpan.FromSeconds(0.5)
                }).GetAwaiter().GetResult();
            });

            t1.Dispose(true);
            t2.Dispose(true);
        }

        [TestMethod]
        public void Transporter_Failes_With_Adapter()
        {
            Init();

            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            t1.FailsWithAdapter = true;
            t1.Connect().Wait();

            var request = new CalculateRequest() { A = 10, B = 15 };

            Assert.ThrowsException<KeyNotFoundException>(() =>
            {
                var response = t1.SendRequest(request).GetAwaiter().GetResult();
            });

            Assert.IsTrue(t1.State == ResonanceComponentState.Failed);
            Assert.IsTrue(t1.FailedStateException.InnerException is KeyNotFoundException);
            Assert.IsTrue(t1.FailedStateException.InnerException == t1.Adapter.FailedStateException);

            t1.Dispose(true);
        }

        [TestMethod]
        public void Transporter_Does_Not_Fail_With_Adapter()
        {
            Init();

            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            t1.FailsWithAdapter = false;
            t1.Connect().Wait();

            var request = new CalculateRequest() { A = 10, B = 15 };

            Assert.ThrowsException<KeyNotFoundException>(() =>
            {
                var response = t1.SendRequest(request).GetAwaiter().GetResult();
            });

            Assert.IsTrue(t1.Adapter.State == ResonanceComponentState.Failed && t1.State == ResonanceComponentState.Connected);

            t1.Dispose(true);
        }

        [TestMethod]
        public void Transporter_Disposes_Adapter()
        {
            Init();

            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            t1.Connect().Wait();
            t1.Dispose(true);

            Assert.IsTrue(t1.State == ResonanceComponentState.Disposed);
            Assert.IsTrue(t1.Adapter.State == ResonanceComponentState.Disposed);
        }

        [TestMethod]
        public void Request_Cancellation_Token_Throws_Exception()
        {
            Init();

            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            ResonanceJsonTransporter t2 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));

            t1.Connect().Wait();
            t2.Connect().Wait();

            CancellationTokenSource cts = new CancellationTokenSource();

            cts.CancelAfter(200);

            Assert.ThrowsException<OperationCanceledException>(() =>
            {
                var response = t1.SendRequest<CalculateRequest, CalculateResponse>(new CalculateRequest(), new ResonanceRequestConfig()
                {
                    CancellationToken = cts.Token,
                }).GetAwaiter().GetResult();
            });

            t1.Dispose(true);
            t2.Dispose(true);
        }

        [TestMethod]
        public void Incorrect_Response_Throws_Exception()
        {
            Init();

            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            ResonanceJsonTransporter t2 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));

            t1.Connect().Wait();
            t2.Connect().Wait();

            t2.RequestReceived += (s, e) =>
            {
                CalculateRequest receivedRequest = e.Request.Message as CalculateRequest; //Should be calculate response...
                t2.SendResponse(new CalculateRequest(), e.Request.Token);
            };

            var request = new CalculateRequest();

            Assert.ThrowsException<InvalidCastException>(() =>
            {
                var response = t1.SendRequest<CalculateRequest, CalculateResponse>(request).GetAwaiter().GetResult();
            });

            t1.Dispose(true);
            t2.Dispose(true);
        }

        [TestMethod]
        public void Decoder_Exception_Throws_Exception()
        {
            Init();

            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            t1.Decoder = new CorruptedDecoder();

            ResonanceJsonTransporter t2 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));

            t1.Connect().Wait();
            t2.Connect().Wait();

            t2.RequestReceived += (s, e) =>
            {
                CalculateRequest receivedRequest = e.Request.Message as CalculateRequest; //Should be calculate response...
                t2.SendResponse(new CalculateResponse(), e.Request.Token);
            };

            var request = new CalculateRequest();

            Assert.ThrowsException<CorruptedDecoderException>(() =>
            {
                var response = t1.SendRequest<CalculateRequest, CalculateResponse>(request).GetAwaiter().GetResult();
            });

            t1.Dispose(true);
            t2.Dispose(true);
        }
    }
}
