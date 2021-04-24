using Microsoft.VisualStudio.TestTools.UnitTesting;
using Resonance.Adapters.WebRTC;
using Resonance.Messages;
using Resonance.Tests.Common;
using Resonance.WebRTC.Exceptions;
using Resonance.WebRTC.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Resonance.Tests
{
    [TestClass]
    [TestCategory("WebRTC")]
    public class WebRTC_TST : ResonanceTest
    {
        #region Helpers

        private byte[] GetRandomByteArray(int sizeInKb)
        {
            Random rnd = new Random();
            byte[] b = new byte[sizeInKb * 1024];
            rnd.NextBytes(b);
            return b;
        }

        public class LargeMessageRequest
        {
            public byte[] Data { get; set; }
        }

        public class LargeMessageResponse
        {
            public byte[] Data { get; set; }
        }

        #endregion

        [TestMethod]
        public void Basic_Test()
        {
            IResonanceTransporter signal1 = ResonanceTransporter.Builder
                .Create()
                .WithInMemoryAdapter()
                .WithAddress("TST")
                .WithJsonTranscoding()
                .Build();

            IResonanceTransporter signal2 = ResonanceTransporter.Builder
                .Create()
                .WithInMemoryAdapter()
                .WithAddress("TST")
                .WithJsonTranscoding()
                .Build();

            signal1.Connect().GetAwaiter().GetResult();
            signal2.Connect().GetAwaiter().GetResult();

            WebRTCAdapter adapter1 = new WebRTCAdapter(signal1, WebRTCAdapterRole.Accept);

            Task.Factory.StartNew(() =>
            {
                adapter1.Connect().GetAwaiter().GetResult();
            });

            Thread.Sleep(100);

            WebRTCAdapter adapter2 = new WebRTCAdapter(signal2, WebRTCAdapterRole.Connect);
            adapter2.Connect().GetAwaiter().GetResult();

            IResonanceTransporter t1 = ResonanceTransporter.Builder
                .Create()
                .WithAdapter(adapter1)
                .WithJsonTranscoding()
                .Build();

            IResonanceTransporter t2 = ResonanceTransporter.Builder
                .Create()
                .WithAdapter(adapter2)
                .WithJsonTranscoding()
                .Build();

            t2.RequestReceived += (x, e) =>
            {
                t2.SendResponse(new CalculateResponse() { Sum = 15 }, e.Request.Token);
            };

            t1.Connect().GetAwaiter().GetResult();
            t2.Connect().GetAwaiter().GetResult();

            var response = t1.SendRequest<CalculateRequest, CalculateResponse>(new CalculateRequest() { A = 10, B = 5 }).GetAwaiter().GetResult();
            Assert.IsTrue(response.Sum == 15);

            t1.Dispose();
            t2.Dispose();
            signal1.Dispose();
            signal2.Dispose();
        }

        [TestMethod]
        public void Create_Adapter_From_Offer()
        {
            IResonanceTransporter signal1 = ResonanceTransporter.Builder
                .Create()
                .WithInMemoryAdapter()
                .WithAddress("TST")
                .WithJsonTranscoding()
                .Build();

            IResonanceTransporter signal2 = ResonanceTransporter.Builder
                .Create()
                .WithInMemoryAdapter()
                .WithAddress("TST")
                .WithJsonTranscoding()
                .Build();

            signal1.Connect().GetAwaiter().GetResult();
            signal2.Connect().GetAwaiter().GetResult();

            WebRTCAdapter adapter1 = null;

            signal1.RegisterRequestHandler<WebRTCOfferRequest>(async (_, request) =>
            {
                adapter1 = new WebRTCAdapter(signal1, request.Message, request.Token);
                await adapter1.Connect();
            });

            WebRTCAdapter adapter2 = new WebRTCAdapter(signal2, WebRTCAdapterRole.Connect);
            adapter2.Connect().GetAwaiter().GetResult();

            IResonanceTransporter t1 = ResonanceTransporter.Builder
                .Create()
                .WithAdapter(adapter1)
                .WithJsonTranscoding()
                .Build();

            IResonanceTransporter t2 = ResonanceTransporter.Builder
                .Create()
                .WithAdapter(adapter2)
                .WithJsonTranscoding()
                .Build();

            t2.RequestReceived += (x, e) =>
            {
                t2.SendResponse(new CalculateResponse() { Sum = 15 }, e.Request.Token);
            };

            t1.Connect().GetAwaiter().GetResult();
            t2.Connect().GetAwaiter().GetResult();

            var response = t1.SendRequest<CalculateRequest, CalculateResponse>(new CalculateRequest() { A = 10, B = 5 }).GetAwaiter().GetResult();
            Assert.IsTrue(response.Sum == 15);

            t1.Dispose();
            t2.Dispose();
            signal1.Dispose();
            signal2.Dispose();
        }

        [TestMethod]
        public void Message_Larger_Than_16_KB_Splits()
        {
            IResonanceTransporter signal1 = ResonanceTransporter.Builder
                .Create()
                .WithInMemoryAdapter()
                .WithAddress("TST")
                .WithJsonTranscoding()
                .Build();

            IResonanceTransporter signal2 = ResonanceTransporter.Builder
                .Create()
                .WithInMemoryAdapter()
                .WithAddress("TST")
                .WithJsonTranscoding()
                .Build();

            signal1.Connect().GetAwaiter().GetResult();
            signal2.Connect().GetAwaiter().GetResult();

            WebRTCAdapter adapter1 = null;

            signal1.RegisterRequestHandler<WebRTCOfferRequest>(async (_, request) =>
            {
                adapter1 = new WebRTCAdapter(signal1, request.Message, request.Token);
                await adapter1.Connect();
            });

            WebRTCAdapter adapter2 = new WebRTCAdapter(signal2, WebRTCAdapterRole.Connect);
            adapter2.Connect().GetAwaiter().GetResult();

            IResonanceTransporter t1 = ResonanceTransporter.Builder
                .Create()
                .WithAdapter(adapter1)
                .WithJsonTranscoding()
                .Build();

            IResonanceTransporter t2 = ResonanceTransporter.Builder
                .Create()
                .WithAdapter(adapter2)
                .WithJsonTranscoding()
                .Build();

            t2.RequestReceived += (x, e) =>
            {
                t2.SendResponse(new LargeMessageResponse() { Data = (e.Request.Message as LargeMessageRequest).Data }, e.Request.Token);
            };

            t1.Connect().GetAwaiter().GetResult();
            t2.Connect().GetAwaiter().GetResult();

            byte[] data = GetRandomByteArray(200);

            var response = t1.SendRequest<LargeMessageRequest, LargeMessageResponse>(new LargeMessageRequest() { Data = data }).GetAwaiter().GetResult();

            Assert.IsTrue(data.SequenceEqual(response.Data));

            t1.Dispose();
            t2.Dispose();
            signal1.Dispose();
            signal2.Dispose();
        }

        [TestMethod]
        public void Stress_Test_With_Large_Data_Volume()
        {
            IResonanceTransporter signal1 = ResonanceTransporter.Builder
                .Create()
                .WithInMemoryAdapter()
                .WithAddress("TST")
                .WithJsonTranscoding()
                .Build();

            IResonanceTransporter signal2 = ResonanceTransporter.Builder
                .Create()
                .WithInMemoryAdapter()
                .WithAddress("TST")
                .WithJsonTranscoding()
                .Build();

            signal1.Connect().GetAwaiter().GetResult();
            signal2.Connect().GetAwaiter().GetResult();

            WebRTCAdapter adapter1 = null;

            signal1.RegisterRequestHandler<WebRTCOfferRequest>(async (_, request) =>
            {
                adapter1 = new WebRTCAdapter(signal1, request.Message, request.Token);
                await adapter1.Connect();
            });

            WebRTCAdapter adapter2 = new WebRTCAdapter(signal2, WebRTCAdapterRole.Connect);
            adapter2.Connect().GetAwaiter().GetResult();

            IResonanceTransporter t1 = ResonanceTransporter.Builder
                .Create()
                .WithAdapter(adapter1)
                .WithJsonTranscoding()
                .Build();

            IResonanceTransporter t2 = ResonanceTransporter.Builder
                .Create()
                .WithAdapter(adapter2)
                .WithJsonTranscoding()
                .Build();

            t2.RequestReceived += (x, e) =>
            {
                t2.SendResponse(new LargeMessageResponse() { Data = (e.Request.Message as LargeMessageRequest).Data }, e.Request.Token);
            };

            t1.Connect().GetAwaiter().GetResult();
            t2.Connect().GetAwaiter().GetResult();

            t1.DefaultRequestTimeout = TimeSpan.FromSeconds(60);

            for (int i = 0; i < 100; i++)
            {
                byte[] data = GetRandomByteArray(60);
                var response = t1.SendRequest<LargeMessageRequest, LargeMessageResponse>(new LargeMessageRequest() { Data = data }).GetAwaiter().GetResult();
                Assert.IsTrue(data.SequenceEqual(response.Data));
            }

            t1.Dispose();
            t2.Dispose();
            signal1.Dispose();
            signal2.Dispose();
        }

        [TestMethod]
        public void Stress_Test_With_Small_Data_Volume()
        {
            IResonanceTransporter signal1 = ResonanceTransporter.Builder
                .Create()
                .WithInMemoryAdapter()
                .WithAddress("TST")
                .WithJsonTranscoding()
                .Build();

            IResonanceTransporter signal2 = ResonanceTransporter.Builder
                .Create()
                .WithInMemoryAdapter()
                .WithAddress("TST")
                .WithJsonTranscoding()
                .Build();

            signal1.Connect().GetAwaiter().GetResult();
            signal2.Connect().GetAwaiter().GetResult();

            WebRTCAdapter adapter1 = null;

            signal1.RegisterRequestHandler<WebRTCOfferRequest>(async (_, request) =>
            {
                adapter1 = new WebRTCAdapter(signal1, request.Message, request.Token);
                await adapter1.Connect();
            });

            WebRTCAdapter adapter2 = new WebRTCAdapter(signal2, WebRTCAdapterRole.Connect);
            adapter2.Connect().GetAwaiter().GetResult();

            IResonanceTransporter t1 = ResonanceTransporter.Builder
                .Create()
                .WithAdapter(adapter1)
                .WithJsonTranscoding()
                .Build();

            IResonanceTransporter t2 = ResonanceTransporter.Builder
                .Create()
                .WithAdapter(adapter2)
                .WithJsonTranscoding()
                .Build();

            t2.RequestReceived += (x, e) =>
            {
                t2.SendResponse(new LargeMessageResponse() { Data = (e.Request.Message as LargeMessageRequest).Data }, e.Request.Token);
            };

            t1.Connect().GetAwaiter().GetResult();
            t2.Connect().GetAwaiter().GetResult();

            t1.DefaultRequestTimeout = TimeSpan.FromSeconds(60);

            for (int i = 0; i < 100; i++)
            {
                byte[] data = GetRandomByteArray(2);
                var response = t1.SendRequest<LargeMessageRequest, LargeMessageResponse>(new LargeMessageRequest() { Data = data }).GetAwaiter().GetResult();
                Assert.IsTrue(data.SequenceEqual(response.Data));
            }

            t1.Dispose();
            t2.Dispose();
            signal1.Dispose();
            signal2.Dispose();
        }

        [TestMethod]
        public void Connection_Timeout_Throws_Exception()
        {
            IResonanceTransporter signal1 = ResonanceTransporter.Builder
                .Create()
                .WithInMemoryAdapter()
                .WithAddress("TST")
                .WithJsonTranscoding()
                .Build();

            IResonanceTransporter signal2 = ResonanceTransporter.Builder
                .Create()
                .WithInMemoryAdapter()
                .WithAddress("TST")
                .WithJsonTranscoding()
                .Build();

            signal1.Connect().GetAwaiter().GetResult();
            signal2.Connect().GetAwaiter().GetResult();

            Thread.Sleep(100);

            WebRTCAdapter adapter2 = new WebRTCAdapter(signal2, WebRTCAdapterRole.Connect);
            adapter2.ConnectionTimeout = TimeSpan.FromSeconds(2);

            Assert.ThrowsException<ResonanceWebRTCConnectionFailedException>(() =>
            {
                adapter2.Connect().GetAwaiter().GetResult();
            });

            adapter2.Dispose();
            signal1.Dispose();
            signal2.Dispose();
        }

        [TestMethod]
        public void Connection_No_Response_Throws_Exception()
        {
            IResonanceTransporter signal2 = ResonanceTransporter.Builder
                .Create()
                .WithInMemoryAdapter()
                .WithAddress("TST")
                .WithJsonTranscoding()
                .Build();

            signal2.Connect().GetAwaiter().GetResult();

            Thread.Sleep(100);

            WebRTCAdapter adapter2 = new WebRTCAdapter(signal2, WebRTCAdapterRole.Connect);
            adapter2.LoggingMode = ResonanceMessageLoggingMode.Title;

            Assert.ThrowsException<ResonanceWebRTCConnectionFailedException>(() =>
            {
                adapter2.Connect().GetAwaiter().GetResult();
            });

            adapter2.Dispose();
            signal2.Dispose();
        }
    }
}
