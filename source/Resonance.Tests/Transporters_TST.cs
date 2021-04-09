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
        public async Task Send_And_Receive_Standard_Request()
        {
            Init();

            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            ResonanceJsonTransporter t2 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));

            await t1.Connect();
            await t2.Connect();

            t2.RequestReceived += async (s, e) =>
            {
                Assert.IsTrue(t1.PendingRequestsCount == 1);
                CalculateRequest receivedRequest = e.Request.Message as CalculateRequest;
                await t2.SendResponse(new CalculateResponse() { Sum = receivedRequest.A + receivedRequest.B }, e.Request.Token);
            };

            var request = new CalculateRequest() { A = 10, B = 15 };
            var response = await t1.SendRequest<CalculateRequest, CalculateResponse>(request);

            await t1.DisposeAsync(true);
            await t2.DisposeAsync(true);

            Assert.AreEqual(response.Sum, request.A + request.B);
        }

        [TestMethod]
        public async Task Send_And_Receive_Standard_Request_With_Error()
        {
            Init();

            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            ResonanceJsonTransporter t2 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));

            await t1.Connect();
            await t2.Connect();

            t2.RequestReceived += async (s, e) =>
            {
                CalculateRequest receivedRequest = e.Request.Message as CalculateRequest;
                await t2.SendErrorResponse("Error Message", e.Request.Token);
            };

            await Assert.ThrowsExceptionAsync<ResonanceResponseException>(async () =>
            {
                var response = await t1.SendRequest<CalculateRequest, CalculateResponse>(new CalculateRequest());
            }, "Error Message");

            await t1.DisposeAsync(true);
            await t2.DisposeAsync(true);
        }

        [TestMethod]
        public async Task Send_And_Receive_Continuous_Request()
        {
            Init();

            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            ResonanceJsonTransporter t2 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));

            await t1.Connect();
            await t2.Connect();

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

            await subscription.WaitAsync();

            Assert.AreEqual(currentValue, request.Count);
            Assert.IsTrue(isCompleted);

            await t1.DisposeAsync(true);
            await t2.DisposeAsync(true);
        }

        [TestMethod]
        public async Task Send_And_Receive_Standard_Request_With_Compression()
        {
            Init();

            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            ResonanceJsonTransporter t2 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));

            t1.Encoder.CompressionConfiguration.Enabled = true;
            t2.Encoder.CompressionConfiguration.Enabled = true;

            await t1.Connect();
            await t2.Connect();

            t2.RequestReceived += async (s, e) =>
            {
                CalculateRequest receivedRequest = e.Request.Message as CalculateRequest;
                await t2.SendResponse(new CalculateResponse() { Sum = receivedRequest.A + receivedRequest.B }, e.Request.Token);
            };

            var request = new CalculateRequest() { A = 10, B = 15 };
            var response = await t1.SendRequest<CalculateRequest, CalculateResponse>(request);

            await t1.DisposeAsync(true);
            await t2.DisposeAsync(true);

            Assert.AreEqual(response.Sum, request.A + request.B);
        }

        [TestMethod]
        public void Send_And_Receive_Standard_Request_With_Encryption()
        {
            Init();

            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            ResonanceJsonTransporter t2 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));

            t1.CryptographyConfiguration.Enabled = true;
            t2.CryptographyConfiguration.Enabled = true;

            t1.Connect().Wait();
            t2.Connect().Wait();

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

            Task.Factory.StartNew(async () =>
            {
                response1 = await t2.SendRequest<CalculateRequest, CalculateResponse>(request);
                response1 = await t2.SendRequest<CalculateRequest, CalculateResponse>(request);
            }).GetAwaiter().GetResult();

            Task.Factory.StartNew(async () =>
            {
                response2 = await t1.SendRequest<CalculateRequest, CalculateResponse>(request);
                response2 = await t1.SendRequest<CalculateRequest, CalculateResponse>(request);
            }).GetAwaiter().GetResult();


            Thread.Sleep(4000);


            t1.Dispose(true);
            t2.Dispose(true);

            Assert.AreEqual(response1.Sum, request.A + request.B);
            Assert.AreEqual(response2.Sum, request.A + request.B);
        }

        [TestMethod]
        public async Task Send_And_Receive_Continuous_Request_With_Error()
        {
            Init();

            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            ResonanceJsonTransporter t2 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));

            await t1.Connect();
            await t2.Connect();

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

            await Assert.ThrowsExceptionAsync<ResonanceResponseException>(async () =>
            {
                await subscription.WaitAsync();
            }, "Test Exception");

            Assert.AreEqual(currentValue, request.Count - 1);
            Assert.IsFalse(isCompleted);
            Assert.IsTrue(hasError);

            await t1.DisposeAsync(true);
            await t2.DisposeAsync(true);
        }

        [TestMethod]
        public async Task Request_Timeout_Throws_Exception()
        {
            Init();

            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            ResonanceJsonTransporter t2 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));

            await t1.Connect();
            await t2.Connect();

            t2.RequestReceived += async (s, e) =>
            {
                Thread.Sleep(1000);
                CalculateRequest receivedRequest = e.Request.Message as CalculateRequest;
                await t2.SendResponse(new CalculateResponse() { Sum = receivedRequest.A + receivedRequest.B }, e.Request.Token);
            };

            var request = new CalculateRequest() { A = 10, B = 15 };

            await Assert.ThrowsExceptionAsync<TimeoutException>(async () =>
            {
                var response = await t1.SendRequest<CalculateRequest, CalculateResponse>(request, new ResonanceRequestConfig()
                {
                    Timeout = TimeSpan.FromSeconds(0.5)
                });
            });

            await t1.DisposeAsync(true);
            await t2.DisposeAsync(true);
        }

        [TestMethod]
        public async Task Transporter_Failes_With_Adapter()
        {
            Init();

            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            t1.FailsWithAdapter = true;
            await t1.Connect();

            var request = new CalculateRequest() { A = 10, B = 15 };

            await Assert.ThrowsExceptionAsync<KeyNotFoundException>(async () =>
            {
                var response = await t1.SendRequest(request);
            });

            Assert.IsTrue(t1.State == ResonanceComponentState.Failed);
            Assert.IsTrue(t1.FailedStateException.InnerException is KeyNotFoundException);
            Assert.IsTrue(t1.FailedStateException.InnerException == t1.Adapter.FailedStateException);

            await t1.DisposeAsync(true);
        }

        [TestMethod]
        public async Task Transporter_Does_Not_Fail_With_Adapter()
        {
            Init();

            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            t1.FailsWithAdapter = false;
            await t1.Connect();

            var request = new CalculateRequest() { A = 10, B = 15 };

            await Assert.ThrowsExceptionAsync<KeyNotFoundException>(async () =>
            {
                var response = await t1.SendRequest(request);
            });

            Assert.IsTrue(t1.Adapter.State == ResonanceComponentState.Failed && t1.State == ResonanceComponentState.Connected);

            await t1.DisposeAsync(true);
        }

        [TestMethod]
        public async Task Transporter_Disposes_Adapter()
        {
            Init();

            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            t1.NotifyOnDisconnect = false; //This is set so the adapter will not fail on disconnect request and thus will not be disposed but failed.
            t1.DisableHandShake = true;

            await t1.Connect();
            await t1.DisposeAsync(true);

            Assert.IsTrue(t1.State == ResonanceComponentState.Disposed, "Transporter was not disposed properly.");
            Assert.IsTrue(t1.Adapter.State == ResonanceComponentState.Disposed, "Adapter was not disposed properly.");
        }

        [TestMethod]
        public async Task Request_Cancellation_Token_Throws_Exception()
        {
            Init();

            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            ResonanceJsonTransporter t2 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));

            await t1.Connect();
            await t2.Connect();

            CancellationTokenSource cts = new CancellationTokenSource();

            cts.CancelAfter(200);

            await Assert.ThrowsExceptionAsync<OperationCanceledException>(async () =>
            {
                var response = await t1.SendRequest<CalculateRequest, CalculateResponse>(new CalculateRequest(), new ResonanceRequestConfig()
                {
                    CancellationToken = cts.Token,
                });
            });

            await t1.DisposeAsync(true);
            await t2.DisposeAsync(true);
        }

        [TestMethod]
        public async Task Incorrect_Response_Throws_Exception()
        {
            Init();

            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            ResonanceJsonTransporter t2 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));

            await t1.Connect();
            await t2.Connect();

            t2.RequestReceived += async (s, e) =>
            {
                CalculateRequest receivedRequest = e.Request.Message as CalculateRequest; //Should be calculate response...
                await t2.SendResponse(new CalculateRequest(), e.Request.Token);
            };

            var request = new CalculateRequest();

            await Assert.ThrowsExceptionAsync<InvalidCastException>(async () =>
            {
                var response = await t1.SendRequest<CalculateRequest, CalculateResponse>(request);
            });

            await t1.DisposeAsync(true);
            await t2.DisposeAsync(true);
        }

        [TestMethod]
        public async Task Decoder_Exception_Throws_Exception()
        {
            Init();

            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            t1.Decoder = new CorruptedDecoder();

            ResonanceJsonTransporter t2 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));

            await t1.Connect();
            await t2.Connect();

            t2.RequestReceived += async (s, e) =>
            {
                CalculateRequest receivedRequest = e.Request.Message as CalculateRequest; //Should be calculate response...
                await t2.SendResponse(new CalculateResponse(), e.Request.Token);
            };

            var request = new CalculateRequest();

            await Assert.ThrowsExceptionAsync<CorruptedDecoderException>(async () =>
            {
                var response = await t1.SendRequest<CalculateRequest, CalculateResponse>(request);
            });

            await t1.DisposeAsync(true);
            await t2.DisposeAsync(true);
        }

        [TestMethod]
        public async Task KeepAlive_Timeout_Fails_Transporter()
        {
            Init();

            if (IsRunningOnAzurePipelines) return;

            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            ResonanceJsonTransporter t2 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));

            t1.DisableHandShake = true;
            t2.DisableHandShake = true;

            t1.DefaultRequestTimeout = TimeSpan.FromSeconds(0.5);

            t1.KeepAliveConfiguration.Enabled = true;
            t1.KeepAliveConfiguration.Retries = 1;
            t1.KeepAliveConfiguration.FailTransporterOnTimeout = true;
            t1.KeepAliveConfiguration.Interval = TimeSpan.FromSeconds(0.5);

            t2.KeepAliveConfiguration.Enabled = false;
            t2.KeepAliveConfiguration.EnableAutoResponse = false;

            await t1.Connect();
            await t2.Connect();

            Thread.Sleep(t1.KeepAliveConfiguration.Delay);

            Thread.Sleep((int)(t1.DefaultRequestTimeout.Add(t1.KeepAliveConfiguration.Interval).TotalMilliseconds * t1.KeepAliveConfiguration.Retries));

            Assert.IsTrue(t1.State == ResonanceComponentState.Failed);
            Assert.IsTrue(t1.FailedStateException is ResonanceKeepAliveException);

            await t1.DisposeAsync(true);
            await t2.DisposeAsync(true);
        }

        [TestMethod]
        public async Task KeepAlive_Timeout_Does_Not_Fails_Transporter()
        {
            Init();

            if (IsRunningOnAzurePipelines) return;

            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            ResonanceJsonTransporter t2 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));

            t1.DisableHandShake = true;
            t2.DisableHandShake = true;

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

            await t1.Connect();
            await t2.Connect();

            Thread.Sleep((int)(t1.DefaultRequestTimeout.Add(t1.KeepAliveConfiguration.Interval).TotalMilliseconds * t1.KeepAliveConfiguration.Retries));

            Assert.IsTrue(t1.State == ResonanceComponentState.Connected && keepAliveFailed);

            await t1.DisposeAsync(true);
            await t2.DisposeAsync(true);
        }

        [TestMethod]
        public async Task KeepAlive_Auto_Response()
        {
            Init();

            if (IsRunningOnAzurePipelines) return;

            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            ResonanceJsonTransporter t2 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));

            t1.DisableHandShake = true;
            t2.DisableHandShake = true;

            t1.DefaultRequestTimeout = TimeSpan.FromSeconds(0.5);

            t1.KeepAliveConfiguration.Enabled = true;
            t1.KeepAliveConfiguration.Retries = 1;
            t1.KeepAliveConfiguration.Delay = TimeSpan.Zero;
            t1.KeepAliveConfiguration.FailTransporterOnTimeout = true;
            t1.KeepAliveConfiguration.Interval = TimeSpan.FromSeconds(0.5);

            t2.KeepAliveConfiguration.Enabled = false;
            t2.KeepAliveConfiguration.EnableAutoResponse = true;

            await t1.Connect();
            await t2.Connect();

            Thread.Sleep((int)(t1.DefaultRequestTimeout.Add(t1.KeepAliveConfiguration.Interval).TotalMilliseconds * 2));
            Assert.IsTrue(t1.State == ResonanceComponentState.Connected);

            await t1.DisposeAsync(true);
            await t2.DisposeAsync(true);

            Thread.Sleep(1000);
        }

        [TestMethod]
        public async Task KeepAlive_Timeout_Retries()
        {
            Init();

            if (IsRunningOnAzurePipelines) return;

            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            ResonanceJsonTransporter t2 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));

            t1.DisableHandShake = true;
            t2.DisableHandShake = true;

            t1.DefaultRequestTimeout = TimeSpan.FromSeconds(0.5);

            t1.KeepAliveConfiguration.Enabled = true;
            t1.KeepAliveConfiguration.Retries = 4;
            t1.KeepAliveConfiguration.Delay = TimeSpan.Zero;
            t1.KeepAliveConfiguration.FailTransporterOnTimeout = true;
            t1.KeepAliveConfiguration.Interval = TimeSpan.FromSeconds(0.5);

            t2.KeepAliveConfiguration.Enabled = false;
            t2.KeepAliveConfiguration.EnableAutoResponse = false;

            await t1.Connect();
            await t2.Connect();

            Thread.Sleep((int)(t1.DefaultRequestTimeout.Add(t1.KeepAliveConfiguration.Interval).TotalMilliseconds * (t1.KeepAliveConfiguration.Retries / 2)));

            Assert.IsTrue(t1.State == ResonanceComponentState.Connected);

            Thread.Sleep((int)(t1.DefaultRequestTimeout.Add(t1.KeepAliveConfiguration.Interval).TotalMilliseconds * (t1.KeepAliveConfiguration.Retries / 2)));

            Thread.Sleep(2000);

            Assert.IsTrue(t1.State == ResonanceComponentState.Failed);
            Assert.IsTrue(t1.FailedStateException is ResonanceKeepAliveException);

            await t1.DisposeAsync(true);
            await t2.DisposeAsync(true);
        }

        [TestMethod]
        public async Task Disconnection_Request()
        {
            Init();

            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            ResonanceJsonTransporter t2 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));

            await t1.Connect();
            await t2.Connect();

            t2.RequestReceived += async (x, e) =>
            {
                await t2.SendResponse(new CalculateResponse(), e.Request.Token);
            };

            await t1.SendRequest<CalculateRequest, CalculateResponse>(new CalculateRequest());

            await t2.Disconnect();

            Thread.Sleep(1000);

            Assert.IsTrue(t1.State == ResonanceComponentState.Failed);
            Assert.IsTrue(t1.FailedStateException is ResonanceConnectionClosedException);

            await t1.DisposeAsync(true);
            await t2.DisposeAsync(true);
        }

        [TestMethod]
        public async Task Send_Object_Without_Expecting_Response()
        {
            Init();

            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            ResonanceJsonTransporter t2 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));

            await t1.Connect();
            await t2.Connect();

            t2.RequestReceived += (s, e) =>
            {
                CalculateRequest receivedRequest = e.Request.Message as CalculateRequest;
                Assert.IsTrue(receivedRequest.A == 10);
            };

            var request = new CalculateRequest() { A = 10 };

            for (int i = 0; i < 1000; i++)
            {
                await t1.SendObject(new CalculateRequest() { A = 10 });
            }

            Assert.IsTrue(t1.PendingRequestsCount == 0);

            await t1.DisposeAsync(true);
            await t2.DisposeAsync(true);
        }

        [TestMethod]
        public async Task Manual_Begin_Handshake()
        {
            Init();

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


            await t1.Connect();
            await t2.Connect();

            await t1.HandShakeNegotiator.BeginHandShakeAsync();
            await t2.HandShakeNegotiator.BeginHandShakeAsync();

            Assert.IsTrue(t1.IsChannelSecure);
            Assert.IsTrue(t2.IsChannelSecure);

            await t1.SendObject(new CalculateRequest());

            await t1.DisposeAsync();
            await t2.DisposeAsync();
        }
    }
}
