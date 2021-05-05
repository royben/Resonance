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
using Resonance.Threading;
using Microsoft.Extensions.Logging;

namespace Resonance.Tests
{
    [TestClass]
    [TestCategory("Transporters")]
    public class Transporters_TST : ResonanceTest
    {
        [TestMethod]
        public void Send_Standard_Message()
        {
            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            ResonanceJsonTransporter t2 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));

            t1.Connect();
            t2.Connect();

            bool received = false;

            t2.MessageReceived += (s, e) =>
            {
                Assert.IsTrue(t1.TotalPendingOutgoingMessages == 0);
                CalculateRequest receivedRequest = e.Message.Object as CalculateRequest;
                Assert.IsTrue(receivedRequest.A == 10 && receivedRequest.B == 15);
                received = true;
            };

            t1.Send(new CalculateRequest() { A = 10, B = 15 });

            Thread.Sleep(500);

            Assert.IsTrue(received);

            t1.Dispose(true);
            t2.Dispose(true);
        }

        [TestMethod]
        public void Send_Standard_Message_With_ACK()
        {
            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            ResonanceJsonTransporter t2 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));

            t1.MessageAcknowledgmentBehavior = ResonanceMessageAckBehavior.ReportErrors;
            t2.MessageAcknowledgmentBehavior = ResonanceMessageAckBehavior.ReportErrors;

            t1.Connect();
            t2.Connect();

            bool received = false;

            t2.MessageReceived += (s, e) =>
            {
                Assert.IsTrue(t1.TotalPendingOutgoingMessages == 1);
                CalculateRequest receivedRequest = e.Message.Object as CalculateRequest;
                Assert.IsTrue(receivedRequest.A == 10 && receivedRequest.B == 15);
                received = true;
            };

            t1.Send(new CalculateRequest() { A = 10, B = 15 }, new ResonanceMessageConfig() { RequireACK = true });

            Thread.Sleep(200);

            Assert.IsTrue(received);
            Assert.IsTrue(t1.TotalPendingOutgoingMessages == 0);

            t1.Dispose(true);
            t2.Dispose(true);
        }

        [TestMethod]
        public void Send_Standard_Message_With_Error_ACK_And_Reporting_Throws_Exception()
        {
            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            ResonanceJsonTransporter t2 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            t2.MessageAcknowledgmentBehavior = ResonanceMessageAckBehavior.ReportErrors;
            t1.MessageAcknowledgmentBehavior = ResonanceMessageAckBehavior.ReportErrors;

            t1.Connect();
            t2.Connect();

            bool received = false;

            t2.MessageReceived += (s, e) =>
            {
                Assert.IsTrue(t1.TotalPendingOutgoingMessages == 1);
                CalculateRequest receivedRequest = e.Message.Object as CalculateRequest;
                Assert.IsTrue(receivedRequest.A == 10 && receivedRequest.B == 15);
                received = true;

                throw new Exception("Test Error");
            };

            Assert.ThrowsException<ResonanceResponseException>(() =>
            {
                t1.Send(new CalculateRequest() { A = 10, B = 15 }, new ResonanceMessageConfig() { RequireACK = true });
            }, "Test Error");

            Thread.Sleep(200);

            Assert.IsTrue(received);
            Assert.IsTrue(t1.TotalPendingOutgoingMessages == 0);

            t1.Dispose(true);
            t2.Dispose(true);
        }

        [TestMethod]
        public void Send_Standard_Message_With_Error_ACK_And_No_Reporting_Does_Not_Throws_Exception()
        {
            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            ResonanceJsonTransporter t2 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));

            t1.Connect();
            t2.Connect();

            bool received = false;

            t2.MessageReceived += (s, e) =>
            {
                CalculateRequest receivedRequest = e.Message.Object as CalculateRequest;
                Assert.IsTrue(receivedRequest.A == 10 && receivedRequest.B == 15);
                received = true;

                throw new Exception("Test Error");
            };

            t1.Send(new CalculateRequest() { A = 10, B = 15 }, new ResonanceMessageConfig() { RequireACK = true });

            Thread.Sleep(200);

            Assert.IsTrue(received);
            Assert.IsTrue(t1.TotalPendingOutgoingMessages == 0);

            t1.Dispose(true);
            t2.Dispose(true);
        }

        [TestMethod]
        public void Send_Standard_Message_ACK_No_x1000()
        {
            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            ResonanceJsonTransporter t2 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));

            t1.Connect();
            t2.Connect();

            ConcurrentList<int> indeces = new ConcurrentList<int>();
            int count = 1;

            Exception exception = null;

            t2.MessageReceived += (s, e) =>
            {
                //Logger.LogInformation($"Received calc {count}");

                try
                {
                    CalculateRequest receivedRequest = e.Message.Object as CalculateRequest;
                    Assert.IsTrue(receivedRequest.A == 10 && receivedRequest.B == 15);
                    indeces.Add(count++);
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
            };

            for (int i = 0; i < 1000; i++)
            {
                t1.Send(new CalculateRequest() { A = 10, B = 15 });
                //Logger.LogInformation($"Sending calc {i}");
            }

            while (count < 1000 && exception == null)
            {
                Thread.Sleep(10);
            }

            if (exception != null)
            {
                Logger.LogError(exception, exception.Message);
            }

            t1.Dispose(true);
            t2.Dispose(true);
        }

        [TestMethod]
        public void Send_Standard_Message_ACK_Yes_x1000()
        {
            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            ResonanceJsonTransporter t2 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));

            t1.Connect();
            t2.Connect();

            ConcurrentList<int> indeces = new ConcurrentList<int>();
            int count = 1;

            Exception exception = null;

            t2.MessageReceived += (s, e) =>
            {
                try
                {
                    CalculateRequest receivedRequest = e.Message.Object as CalculateRequest;
                    Assert.IsTrue(receivedRequest.A == 10 && receivedRequest.B == 15);
                    indeces.Add(count++);
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
            };

            for (int i = 0; i < 1000; i++)
            {
                t1.Send(new CalculateRequest() { A = 10, B = 15 },new ResonanceMessageConfig() { RequireACK = true });
            }

            while (count < 1000 && exception == null)
            {
                Thread.Sleep(10);
            }

            if (exception != null)
            {
                Logger.LogError(exception, exception.Message);
            }

            t1.Dispose(true);
            t2.Dispose(true);
        }

        [TestMethod]
        public void Send_And_Receive_Standard_Request()
        {
            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            ResonanceJsonTransporter t2 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));

            t1.Connect();
            t2.Connect();

            t2.RequestReceived += (s, e) =>
            {
                Assert.IsTrue(t1.TotalPendingOutgoingMessages == 1);
                CalculateRequest receivedRequest = e.Message.Object as CalculateRequest;
                t2.SendResponse(new CalculateResponse() { Sum = receivedRequest.A + receivedRequest.B }, e.Message.Token);
            };

            var request = new CalculateRequest() { A = 10, B = 15 };
            var response = t1.SendRequest<CalculateRequest, CalculateResponse>(request, new ResonanceRequestConfig() { LoggingMode = ResonanceMessageLoggingMode.Content });

            t1.Dispose(true);
            t2.Dispose(true);

            Assert.AreEqual(response.Sum, request.A + request.B);
        }

        [TestMethod]
        public void Send_And_Receive_Standard_Request_With_Error()
        {
            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            ResonanceJsonTransporter t2 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));

            t1.Connect();
            t2.Connect();

            t2.RequestReceived += (s, e) =>
            {
                CalculateRequest receivedRequest = e.Message.Object as CalculateRequest;
                t2.SendErrorResponse("Error Message", e.Message.Token);
            };

            Assert.ThrowsException<ResonanceResponseException>(() =>
            {
                var response = t1.SendRequest<CalculateRequest, CalculateResponse>(new CalculateRequest());
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

            t1.Connect();
            t2.Connect();

            t2.RequestReceived += (s, e) =>
            {
                ProgressRequest receivedRequest = e.Message.Object as ProgressRequest;

                Task.Factory.StartNew(() =>
                {
                    for (int i = 0; i < receivedRequest.Count; i++)
                    {
                        t2.SendResponse(new ProgressResponse() { Value = i }, e.Message.Token);
                        Thread.Sleep(receivedRequest.Interval);
                    }

                    t2.SendResponse(new ProgressResponse() { Value = receivedRequest.Count }, e.Message.Token, new ResonanceResponseConfig()
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

            subscription.Wait();

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

            t1.Connect();
            t2.Connect();

            t2.RequestReceived += (s, e) =>
            {
                CalculateRequest receivedRequest = e.Message.Object as CalculateRequest;
                t2.SendResponse(new CalculateResponse() { Sum = receivedRequest.A + receivedRequest.B }, e.Message.Token);
            };

            var request = new CalculateRequest() { A = 10, B = 15 };
            var response = t1.SendRequest<CalculateRequest, CalculateResponse>(request);

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

            t1.Connect();
            t2.Connect();

            t2.RequestReceived += (s, e) =>
            {
                CalculateRequest receivedRequest = e.Message.Object as CalculateRequest;
                t2.SendResponse(new CalculateResponse() { Sum = receivedRequest.A + receivedRequest.B }, e.Message.Token);
            };

            t1.RequestReceived += (s, e) =>
            {
                CalculateRequest receivedRequest = e.Message.Object as CalculateRequest;
                t1.SendResponse(new CalculateResponse() { Sum = receivedRequest.A + receivedRequest.B }, e.Message.Token);
            };

            var request = new CalculateRequest() { A = 10, B = 15 };

            CalculateResponse response1 = null;
            CalculateResponse response2 = null;

            Task.Factory.StartNew(() =>
            {
                response1 = t2.SendRequest<CalculateRequest, CalculateResponse>(request);
                response1 = t2.SendRequest<CalculateRequest, CalculateResponse>(request);
            }).GetAwaiter().GetResult();

            Task.Factory.StartNew(() =>
            {
                response2 = t1.SendRequest<CalculateRequest, CalculateResponse>(request);
                response2 = t1.SendRequest<CalculateRequest, CalculateResponse>(request);
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

            t1.Connect();
            t2.Connect();

            t2.RequestReceived += (s, e) =>
            {
                ProgressRequest receivedRequest = e.Message.Object as ProgressRequest;

                Task.Factory.StartNew(() =>
                {
                    for (int i = 0; i < receivedRequest.Count; i++)
                    {
                        t2.SendResponse(new ProgressResponse() { Value = i }, e.Message.Token);
                        Thread.Sleep(receivedRequest.Interval);
                    }

                    t2.SendErrorResponse("Test Exception", e.Message.Token);

                    Thread.Sleep(receivedRequest.Interval);

                    t2.SendResponse(new ProgressResponse() { Value = receivedRequest.Count }, e.Message.Token, new ResonanceResponseConfig()
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
                subscription.Wait();
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

            t1.Connect();
            t2.Connect();

            t2.RequestReceived += (s, e) =>
            {
                Thread.Sleep(1000);
                CalculateRequest receivedRequest = e.Message.Object as CalculateRequest;
                t2.SendResponse(new CalculateResponse() { Sum = receivedRequest.A + receivedRequest.B }, e.Message.Token);
            };

            var request = new CalculateRequest() { A = 10, B = 15 };

            Assert.ThrowsException<TimeoutException>(() =>
            {
                var response = t1.SendRequest<CalculateRequest, CalculateResponse>(request, new ResonanceRequestConfig()
                {
                    Timeout = TimeSpan.FromSeconds(0.5)
                });
            });

            t1.Dispose(true);
            t2.Dispose(true);
        }

        [TestMethod]
        public void Transporter_Failes_With_Adapter()
        {
            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            t1.FailsWithAdapter = true;
            t1.Connect();

            var request = new CalculateRequest() { A = 10, B = 15 };


            try
            {
                var response = t1.SendRequest(request);
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
            t1.Connect();

            var request = new CalculateRequest() { A = 10, B = 15 };

            Assert.ThrowsException<KeyNotFoundException>(() =>
            {
                var response = t1.SendRequest(request);
            });

            Assert.IsTrue(t1.Adapter.State == ResonanceComponentState.Failed && t1.State == ResonanceComponentState.Connected);

            t1.Dispose(true);
        }

        [TestMethod]
        public void Transporter_Disposes_Adapter()
        {
            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            t1.NotifyOnDisconnect = false; //This is set so the adapter will not fail on disconnect request and thus will not be disposed but failed.

            t1.Connect();
            t1.Dispose(true);

            Assert.IsTrue(t1.State == ResonanceComponentState.Disposed, "Transporter was not disposed properly.");
            Assert.IsTrue(t1.Adapter.State == ResonanceComponentState.Disposed, "Adapter was not disposed properly.");
        }

        [TestMethod]
        public void Request_Cancellation_Token_Throws_Exception()
        {
            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            ResonanceJsonTransporter t2 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));

            t1.Connect();
            t2.Connect();

            CancellationTokenSource cts = new CancellationTokenSource();

            cts.CancelAfter(200);

            Assert.ThrowsException<OperationCanceledException>(() =>
            {
                var response = t1.SendRequest<CalculateRequest, CalculateResponse>(new CalculateRequest(), new ResonanceRequestConfig()
                {
                    CancellationToken = cts.Token,
                });
            });

            t1.Dispose(true);
            t2.Dispose(true);
        }

        [TestMethod]
        public void Incorrect_Response_Throws_Exception()
        {
            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            ResonanceJsonTransporter t2 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));

            t1.Connect();
            t2.Connect();

            t2.RequestReceived += (s, e) =>
            {
                CalculateRequest receivedRequest = e.Message.Object as CalculateRequest; //Should be calculate response...
                t2.SendResponse(new CalculateRequest(), e.Message.Token);
            };

            var request = new CalculateRequest();

            Assert.ThrowsException<InvalidCastException>(() =>
            {
                var response = t1.SendRequest<CalculateRequest, CalculateResponse>(request);
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

            t1.Connect();
            t2.Connect();

            t2.RequestReceived += (s, e) =>
            {
                CalculateRequest receivedRequest = e.Message.Object as CalculateRequest; //Should be calculate response...
                t2.SendResponse(new CalculateResponse(), e.Message.Token);
            };

            var request = new CalculateRequest();

            Assert.ThrowsException<CorruptedDecoderException>(() =>
            {
                var response = t1.SendRequest<CalculateRequest, CalculateResponse>(request);
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

            t1.Connect();
            t2.Connect();

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

            t1.Connect();
            t2.Connect();

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

            t1.Connect();
            t2.Connect();

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

            t1.Connect();
            t2.Connect();

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

            t1.Connect();
            t2.Connect();

            t2.RequestReceived += (x, e) =>
            {
                t2.SendResponseAsync(new CalculateResponse(), e.Message.Token);
            };

            t1.SendRequest<CalculateRequest, CalculateResponse>(new CalculateRequest());

            t2.Disconnect();

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

            t1.Connect();
            t2.Connect();

            t2.RequestReceived += (s, e) =>
            {
                CalculateRequest receivedRequest = e.Message.Object as CalculateRequest;
                Assert.IsTrue(receivedRequest.A == 10);
            };

            var request = new CalculateRequest() { A = 10 };

            for (int i = 0; i < 1000; i++)
            {
                t1.Send(new CalculateRequest() { A = 10 });
            }

            Assert.IsTrue(t1.TotalPendingOutgoingMessages == 0);

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


            t1.Connect();
            t2.Connect();

            t1.HandShakeNegotiator.BeginHandShake();
            t2.HandShakeNegotiator.BeginHandShake();

            Assert.IsTrue(t1.IsChannelSecure);
            Assert.IsTrue(t2.IsChannelSecure);

            t1.Send(new CalculateRequest());

            t1.Dispose();
            t2.Dispose();
        }
    }
}
