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
            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            ResonanceJsonTransporter t2 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));

            t1.Connect().GetAwaiter().GetResult();
            t2.Connect().GetAwaiter().GetResult();

            t2.RequestReceived += (s, e) =>
            {
                Assert.IsTrue(t1.PendingRequestsCount == 1);
                CalculateRequest receivedRequest = e.Request.Message as CalculateRequest;
                t2.SendResponse(new CalculateResponse() { Sum = receivedRequest.A + receivedRequest.B }, e.Request.Token).GetAwaiter().GetResult();
            };

            var request = new CalculateRequest() { A = 10, B = 15 };
            var response = t1.SendRequest<CalculateRequest, CalculateResponse>(request, new ResonanceRequestConfig() { LoggingMode = ResonanceMessageLoggingMode.Content }).GetAwaiter().GetResult();

            t1.Dispose(true);
            t2.Dispose(true);

            Assert.AreEqual(response.Sum, request.A + request.B);
        }

        [TestMethod]
        public void Send_And_Receive_Standard_Request_With_Error()
        {
            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            ResonanceJsonTransporter t2 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));

            t1.Connect().GetAwaiter().GetResult();
            t2.Connect().GetAwaiter().GetResult();

            t2.RequestReceived += (s, e) =>
            {
                CalculateRequest receivedRequest = e.Request.Message as CalculateRequest;
                t2.SendErrorResponse("Error Message", e.Request.Token).GetAwaiter().GetResult();
            };

            Assert.ThrowsException<ResonanceResponseException>(() =>
            {
                var response = t1.SendRequest<CalculateRequest, CalculateResponse>(new CalculateRequest()).GetAwaiter().GetResult();
            }, "Error Message");

            t1.Dispose(true);
            t2.Dispose(true);

            Dispose();
        }

        [TestMethod]
        public void Send_And_Receive_Continuous_Request()
        {
            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            ResonanceJsonTransporter t2 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));

            t1.Connect().GetAwaiter().GetResult();
            t2.Connect().GetAwaiter().GetResult();

            t2.RequestReceived += (s, e) =>
            {
                ProgressRequest receivedRequest = e.Request.Message as ProgressRequest;

                Task.Factory.StartNew(() =>
                {
                    for (int i = 0; i < receivedRequest.Count; i++)
                    {
                        t2.SendResponse(new ProgressResponse() { Value = i }, e.Request.Token).GetAwaiter().GetResult();
                        Thread.Sleep(receivedRequest.Interval);
                    }

                    t2.SendResponse(new ProgressResponse() { Value = receivedRequest.Count }, e.Request.Token, new ResonanceResponseConfig()
                    {
                        Completed = true
                    }).GetAwaiter().GetResult();

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

            subscription.WaitAsync().GetAwaiter().GetResult();

            Assert.AreEqual(currentValue, request.Count);
            Assert.IsTrue(isCompleted);

            t1.Dispose(true);
            t2.Dispose(true);
        }

        [TestMethod]
        public void Send_And_Receive_Standard_Request_With_Compression()
        {
            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            ResonanceJsonTransporter t2 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));

            t1.Encoder.CompressionConfiguration.Enabled = true;
            t2.Encoder.CompressionConfiguration.Enabled = true;

            t1.Connect().GetAwaiter().GetResult();
            t2.Connect().GetAwaiter().GetResult();

            t2.RequestReceived += (s, e) =>
            {
                CalculateRequest receivedRequest = e.Request.Message as CalculateRequest;
                t2.SendResponse(new CalculateResponse() { Sum = receivedRequest.A + receivedRequest.B }, e.Request.Token).GetAwaiter().GetResult();
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
            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            ResonanceJsonTransporter t2 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));

            t1.CryptographyConfiguration.Enabled = true;
            t2.CryptographyConfiguration.Enabled = true;

            t1.Connect().GetAwaiter().GetResult();
            t2.Connect().GetAwaiter().GetResult();

            t2.RequestReceived += (s, e) =>
            {
                CalculateRequest receivedRequest = e.Request.Message as CalculateRequest;
                t2.SendResponse(new CalculateResponse() { Sum = receivedRequest.A + receivedRequest.B }, e.Request.Token);
            };

            t1.RequestReceived += (s, e) =>
            {
                CalculateRequest receivedRequest = e.Request.Message as CalculateRequest;
                t1.SendResponse(new CalculateResponse() { Sum = receivedRequest.A + receivedRequest.B }, e.Request.Token);
            };

            var request = new CalculateRequest() { A = 10, B = 15 };

            CalculateResponse response1 = null;
            CalculateResponse response2 = null;

            Task.Factory.StartNew(() =>
            {
                response1 = t2.SendRequest<CalculateRequest, CalculateResponse>(request).GetAwaiter().GetResult();
                response1 = t2.SendRequest<CalculateRequest, CalculateResponse>(request).GetAwaiter().GetResult();
            }).GetAwaiter().GetResult();

            Task.Factory.StartNew(() =>
            {
                response2 = t1.SendRequest<CalculateRequest, CalculateResponse>(request).GetAwaiter().GetResult();
                response2 = t1.SendRequest<CalculateRequest, CalculateResponse>(request).GetAwaiter().GetResult();
            }).GetAwaiter().GetResult();


            Thread.Sleep(4000);


            t1.Dispose(true);
            t2.Dispose(true);

            Assert.AreEqual(response1.Sum, request.A + request.B);
            Assert.AreEqual(response2.Sum, request.A + request.B);
        }

        [TestMethod]
        public void Send_And_Receive_Continuous_Request_With_Error()
        {
            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            ResonanceJsonTransporter t2 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));

            t1.Connect().GetAwaiter().GetResult();
            t2.Connect().GetAwaiter().GetResult();

            t2.RequestReceived += (s, e) =>
            {
                ProgressRequest receivedRequest = e.Request.Message as ProgressRequest;

                Task.Factory.StartNew(() =>
                {
                    for (int i = 0; i < receivedRequest.Count; i++)
                    {
                        t2.SendResponse(new ProgressResponse() { Value = i }, e.Request.Token).GetAwaiter().GetResult();
                        Thread.Sleep(receivedRequest.Interval);
                    }

                    t2.SendErrorResponse("Test Exception", e.Request.Token).GetAwaiter().GetResult();

                    Thread.Sleep(receivedRequest.Interval);

                    t2.SendResponse(new ProgressResponse() { Value = receivedRequest.Count }, e.Request.Token, new ResonanceResponseConfig()
                    {
                        Completed = true
                    }).GetAwaiter().GetResult();
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
            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            ResonanceJsonTransporter t2 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));

            t1.Connect().GetAwaiter().GetResult();
            t2.Connect().GetAwaiter().GetResult();

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
            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            t1.FailsWithAdapter = true;
            t1.Connect().GetAwaiter().GetResult();

            var request = new CalculateRequest() { A = 10, B = 15 };


            try
            {
                var response = t1.SendRequest(request).GetAwaiter().GetResult();
                Assert.Fail("Expected an exception.");
            }
            catch { }

            Assert.IsTrue(t1.State == ResonanceComponentState.Failed);
            Assert.IsTrue(t1.FailedStateException.InnerException is KeyNotFoundException);
            Assert.IsTrue(t1.FailedStateException.InnerException == t1.Adapter.FailedStateException);

            t1.Dispose(true);
        }

        [TestMethod]
        public void Transporter_Does_Not_Fail_With_Adapter()
        {
            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            t1.FailsWithAdapter = false;
            t1.Connect().GetAwaiter().GetResult();

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
            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            t1.NotifyOnDisconnect = false; //This is set so the adapter will not fail on disconnect request and thus will not be disposed but failed.

            t1.Connect().GetAwaiter().GetResult();
            t1.Dispose(true);

            Assert.IsTrue(t1.State == ResonanceComponentState.Disposed, "Transporter was not disposed properly.");
            Assert.IsTrue(t1.Adapter.State == ResonanceComponentState.Disposed, "Adapter was not disposed properly.");
        }

        [TestMethod]
        public void Request_Cancellation_Token_Throws_Exception()
        {
            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            ResonanceJsonTransporter t2 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));

            t1.Connect().GetAwaiter().GetResult();
            t2.Connect().GetAwaiter().GetResult();

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
            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            ResonanceJsonTransporter t2 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));

            t1.Connect().GetAwaiter().GetResult();
            t2.Connect().GetAwaiter().GetResult();

            t2.RequestReceived += (s, e) =>
            {
                CalculateRequest receivedRequest = e.Request.Message as CalculateRequest; //Should be calculate response...
                t2.SendResponse(new CalculateRequest(), e.Request.Token).GetAwaiter().GetResult();
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
            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            t1.Decoder = new CorruptedDecoder();

            ResonanceJsonTransporter t2 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));

            t1.Connect().GetAwaiter().GetResult();
            t2.Connect().GetAwaiter().GetResult();

            t2.RequestReceived += (s, e) =>
            {
                CalculateRequest receivedRequest = e.Request.Message as CalculateRequest; //Should be calculate response...
                t2.SendResponse(new CalculateResponse(), e.Request.Token).GetAwaiter().GetResult();
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

            t1.Connect().GetAwaiter().GetResult();
            t2.Connect().GetAwaiter().GetResult();

            Thread.Sleep(t1.KeepAliveConfiguration.Delay);

            Thread.Sleep((int)(t1.DefaultRequestTimeout.Add(t1.KeepAliveConfiguration.Interval).TotalMilliseconds * t1.KeepAliveConfiguration.Retries));

            Assert.IsTrue(t1.State == ResonanceComponentState.Failed);
            Assert.IsTrue(t1.FailedStateException is ResonanceKeepAliveException);

            t1.Dispose(true);
            t2.Dispose(true);
        }

        [TestMethod]
        public void KeepAlive_Timeout_Does_Not_Fails_Transporter()
        {
            if (IsRunningOnAzurePipelines) return;

            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            ResonanceJsonTransporter t2 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));

            t1.DefaultRequestTimeout = TimeSpan.FromSeconds(0.5);

            bool keepAliveFailed = false;

            t1.KeepAliveConfiguration.Enabled = true;
            t1.KeepAliveConfiguration.Retries = 1;
            t1.KeepAliveConfiguration.Delay = TimeSpan.Zero;
            t1.KeepAliveConfiguration.FailTransporterOnTimeout = false;
            t1.KeepAliveConfiguration.Interval = TimeSpan.FromSeconds(0.5);

            t1.KeepAliveFailed += (_, __) =>
            {
                keepAliveFailed = true;
            };

            t2.KeepAliveConfiguration.Enabled = false;
            t2.KeepAliveConfiguration.EnableAutoResponse = false;

            t1.Connect().GetAwaiter().GetResult();
            t2.Connect().GetAwaiter().GetResult();

            Thread.Sleep((int)(t1.DefaultRequestTimeout.Add(t1.KeepAliveConfiguration.Interval).TotalMilliseconds * t1.KeepAliveConfiguration.Retries));

            Assert.IsTrue(t1.State == ResonanceComponentState.Connected && keepAliveFailed);

            t1.Dispose(true);
            t2.Dispose(true);
        }

        [TestMethod]
        public void KeepAlive_Auto_Response()
        {
            if (IsRunningOnAzurePipelines) return;

            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            ResonanceJsonTransporter t2 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));

            t1.DefaultRequestTimeout = TimeSpan.FromSeconds(0.5);

            t1.KeepAliveConfiguration.Enabled = true;
            t1.KeepAliveConfiguration.Retries = 1;
            t1.KeepAliveConfiguration.Delay = TimeSpan.Zero;
            t1.KeepAliveConfiguration.FailTransporterOnTimeout = true;
            t1.KeepAliveConfiguration.Interval = TimeSpan.FromSeconds(0.5);

            t2.KeepAliveConfiguration.Enabled = false;
            t2.KeepAliveConfiguration.EnableAutoResponse = true;

            t1.Connect().GetAwaiter().GetResult();
            t2.Connect().GetAwaiter().GetResult();

            Thread.Sleep((int)(t1.DefaultRequestTimeout.Add(t1.KeepAliveConfiguration.Interval).TotalMilliseconds * 2));
            Assert.IsTrue(t1.State == ResonanceComponentState.Connected);

            t1.Dispose(true);
            t2.Dispose(true);

            Thread.Sleep(1000);
        }

        [TestMethod]
        public void KeepAlive_Timeout_Retries()
        {
            if (IsRunningOnAzurePipelines) return;

            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            ResonanceJsonTransporter t2 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));

            t1.DefaultRequestTimeout = TimeSpan.FromSeconds(0.5);

            t1.KeepAliveConfiguration.Enabled = true;
            t1.KeepAliveConfiguration.Retries = 4;
            t1.KeepAliveConfiguration.Delay = TimeSpan.Zero;
            t1.KeepAliveConfiguration.FailTransporterOnTimeout = true;
            t1.KeepAliveConfiguration.Interval = TimeSpan.FromSeconds(0.5);

            t2.KeepAliveConfiguration.Enabled = false;
            t2.KeepAliveConfiguration.EnableAutoResponse = false;

            t1.Connect().GetAwaiter().GetResult();
            t2.Connect().GetAwaiter().GetResult();

            Thread.Sleep((int)(t1.DefaultRequestTimeout.Add(t1.KeepAliveConfiguration.Interval).TotalMilliseconds * (t1.KeepAliveConfiguration.Retries / 2)));

            Assert.IsTrue(t1.State == ResonanceComponentState.Connected);

            Thread.Sleep((int)(t1.DefaultRequestTimeout.Add(t1.KeepAliveConfiguration.Interval).TotalMilliseconds * (t1.KeepAliveConfiguration.Retries / 2)));

            Thread.Sleep(2000);

            Assert.IsTrue(t1.State == ResonanceComponentState.Failed);
            Assert.IsTrue(t1.FailedStateException is ResonanceKeepAliveException);

            t1.Dispose(true);
            t2.Dispose(true);

            Dispose();
        }

        [TestMethod]
        public void Disconnection_Request()
        {
            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            ResonanceJsonTransporter t2 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));

            t1.Connect().GetAwaiter().GetResult();
            t2.Connect().GetAwaiter().GetResult();

            t2.RequestReceived += (x, e) =>
            {
                t2.SendResponse(new CalculateResponse(), e.Request.Token);
            };

            t1.SendRequest<CalculateRequest, CalculateResponse>(new CalculateRequest()).GetAwaiter().GetResult();

            t2.Disconnect().GetAwaiter().GetResult();

            Thread.Sleep(1000);

            Assert.IsTrue(t1.State == ResonanceComponentState.Failed);
            Assert.IsTrue(t1.FailedStateException is ResonanceConnectionClosedException);

            t1.Dispose(true);
            t2.Dispose(true);
        }

        [TestMethod]
        public void Send_Object_Without_Expecting_Response()
        {
            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            ResonanceJsonTransporter t2 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));

            t1.Connect().GetAwaiter().GetResult();
            t2.Connect().GetAwaiter().GetResult();

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

        [TestMethod]
        public void Manual_Begin_Handshake()
        {
            var t1 = ResonanceTransporter.Builder
                .Create()
                .WithInMemoryAdapter()
                .WithAddress("TST")
                .WithJsonTranscoding()
                .NoKeepAlive()
                .WithEncryption()
                .Build();

            var t2 = ResonanceTransporter.Builder
                .Create()
                .WithInMemoryAdapter()
                .WithAddress("TST")
                .WithJsonTranscoding()
                .NoKeepAlive()
                .WithEncryption()
                .Build();


            t1.Connect().GetAwaiter().GetResult();
            t2.Connect().GetAwaiter().GetResult();

            t1.HandShakeNegotiator.BeginHandShake();
            t2.HandShakeNegotiator.BeginHandShake();

            Assert.IsTrue(t1.IsChannelSecure);
            Assert.IsTrue(t2.IsChannelSecure);

            t1.SendObject(new CalculateRequest()).GetAwaiter().GetResult();

            t1.Dispose();
            t2.Dispose();
        }
    }
}
