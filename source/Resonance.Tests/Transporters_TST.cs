using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Resonance.Adapters;
using Resonance.Adapters.InMemory;
using Resonance.Exceptions;
using Resonance.Tests.Common;
using Resonance.Messages;
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
                Assert.IsTrue(t1.PendingRequestsCount == 1);
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

            t1.Encoder.CompressionConfiguration.Enabled = true;
            t2.Encoder.CompressionConfiguration.Enabled = true;

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
        public void Send_And_Receive_Standard_Request_With_Encryption()
        {
            Init();

            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            ResonanceJsonTransporter t2 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));

            t1.Encoder.EncryptionConfiguration.Enabled = true;
            t2.Encoder.EncryptionConfiguration.Enabled = true;

            t1.Encoder.EncryptionConfiguration.SetSymmetricAlgorithmPassword("Roy");
            t1.Decoder.EncryptionConfiguration.SetSymmetricAlgorithmPassword("Roy");
            t2.Encoder.EncryptionConfiguration.SetSymmetricAlgorithmPassword("Roy");
            t2.Decoder.EncryptionConfiguration.SetSymmetricAlgorithmPassword("Roy");

            t1.Encoder.CompressionConfiguration.Enabled = true;
            t2.Encoder.CompressionConfiguration.Enabled = true;

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

        [TestMethod]
        public void KeepAlive_Timeout_Fails_Transporter()
        {
            Init();

            if (IsRunningOnAzurePipelines) return;

            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            ResonanceJsonTransporter t2 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));

            t1.DefaultRequestTimeout = TimeSpan.FromSeconds(0.5);

            t1.KeepAliveConfiguration.Enabled = true;
            t1.KeepAliveConfiguration.Retries = 1;
            t1.KeepAliveConfiguration.FailTransporterOnTimeout = true;
            t1.KeepAliveConfiguration.Interval = TimeSpan.FromSeconds(0.5);

            t2.KeepAliveConfiguration.Enabled = false;
            t2.KeepAliveConfiguration.EnableAutoResponse = false;

            t1.Connect().Wait();
            t2.Connect().Wait();

            Thread.Sleep((int)(t1.DefaultRequestTimeout.Add(t1.KeepAliveConfiguration.Interval).TotalMilliseconds * t1.KeepAliveConfiguration.Retries));

            Assert.IsTrue(t1.State == ResonanceComponentState.Failed);
            Assert.IsTrue(t1.FailedStateException is ResonanceKeepAliveException);

            t1.Dispose(true);
            t2.Dispose(true);
        }

        [TestMethod]
        public void KeepAlive_Timeout_Does_Not_Fails_Transporter()
        {
            Init();

            if (IsRunningOnAzurePipelines) return;

            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            ResonanceJsonTransporter t2 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));

            t1.DefaultRequestTimeout = TimeSpan.FromSeconds(0.5);

            bool keepAliveFailed = false;

            t1.KeepAliveConfiguration.Enabled = true;
            t1.KeepAliveConfiguration.Retries = 1;
            t1.KeepAliveConfiguration.FailTransporterOnTimeout = false;
            t1.KeepAliveConfiguration.Interval = TimeSpan.FromSeconds(0.5);

            t1.KeepAliveFailed += (_, __) =>
            {
                keepAliveFailed = true;
            };

            t2.KeepAliveConfiguration.Enabled = false;
            t2.KeepAliveConfiguration.EnableAutoResponse = false;

            t1.Connect().Wait();
            t2.Connect().Wait();

            Thread.Sleep((int)(t1.DefaultRequestTimeout.Add(t1.KeepAliveConfiguration.Interval).TotalMilliseconds * t1.KeepAliveConfiguration.Retries));

            Assert.IsTrue(t1.State == ResonanceComponentState.Connected && keepAliveFailed);

            t1.Dispose(true);
            t2.Dispose(true);
        }

        [TestMethod]
        public void KeepAlive_Auto_Response()
        {
            Init();

            if (IsRunningOnAzurePipelines) return;

            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            ResonanceJsonTransporter t2 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));

            t1.DefaultRequestTimeout = TimeSpan.FromSeconds(0.5);

            t1.KeepAliveConfiguration.Enabled = true;
            t1.KeepAliveConfiguration.Retries = 1;
            t1.KeepAliveConfiguration.FailTransporterOnTimeout = true;
            t1.KeepAliveConfiguration.Interval = TimeSpan.FromSeconds(0.5);

            t2.KeepAliveConfiguration.Enabled = false;
            t2.KeepAliveConfiguration.EnableAutoResponse = true;

            t1.Connect().Wait();
            t2.Connect().Wait();

            Thread.Sleep((int)(t1.DefaultRequestTimeout.Add(t1.KeepAliveConfiguration.Interval).TotalMilliseconds * 2));
            Assert.IsTrue(t1.State == ResonanceComponentState.Connected);

            t1.Dispose(true);
            t2.Dispose(true);
        }

        [TestMethod]
        public void KeepAlive_Timeout_Retries()
        {
            Init();

            if (IsRunningOnAzurePipelines) return;

            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            ResonanceJsonTransporter t2 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));

            t1.DefaultRequestTimeout = TimeSpan.FromSeconds(0.5);

            t1.KeepAliveConfiguration.Enabled = true;
            t1.KeepAliveConfiguration.Retries = 4;
            t1.KeepAliveConfiguration.FailTransporterOnTimeout = true;
            t1.KeepAliveConfiguration.Interval = TimeSpan.FromSeconds(0.5);

            t2.KeepAliveConfiguration.Enabled = false;
            t2.KeepAliveConfiguration.EnableAutoResponse = false;

            t1.Connect().Wait();
            t2.Connect().Wait();

            Thread.Sleep((int)(t1.DefaultRequestTimeout.Add(t1.KeepAliveConfiguration.Interval).TotalMilliseconds * (t1.KeepAliveConfiguration.Retries / 2)));

            Assert.IsTrue(t1.State == ResonanceComponentState.Connected);

            Thread.Sleep((int)(t1.DefaultRequestTimeout.Add(t1.KeepAliveConfiguration.Interval).TotalMilliseconds * (t1.KeepAliveConfiguration.Retries / 2)));

            Assert.IsTrue(t1.State == ResonanceComponentState.Failed);
            Assert.IsTrue(t1.FailedStateException is ResonanceKeepAliveException);

            t1.Dispose(true);
            t2.Dispose(true);
        }

        [TestMethod]
        public void Disconnection_Request()
        {
            Init();

            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            ResonanceJsonTransporter t2 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));

            t1.Connect().Wait();
            t2.Connect().Wait();

            t2.Disconnect().Wait();

            Thread.Sleep(1000);

            Assert.IsTrue(t1.State == ResonanceComponentState.Failed);
            Assert.IsTrue(t1.FailedStateException is ResonanceConnectionClosedException);

            t1.Dispose(true);
            t2.Dispose(true);
        }

        [TestMethod]
        public void Send_Object_Without_Expecting_Response()
        {
            Init();

            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            ResonanceJsonTransporter t2 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));

            t1.Connect().Wait();
            t2.Connect().Wait();

            t2.RequestReceived += (s, e) =>
            {
                CalculateRequest receivedRequest = e.Request.Message as CalculateRequest;
                Assert.IsTrue(receivedRequest.A == 10);
            };

            var request = new CalculateRequest() { A = 10 };

            for (int i = 0; i < 1000; i++)
            {
                t1.SendObject(new CalculateRequest() { A = 10 }).GetAwaiter().GetResult();
            }

            Assert.IsTrue(t1.PendingRequestsCount == 0);

            t1.Dispose(true);
            t2.Dispose(true);
        }
    }
}
