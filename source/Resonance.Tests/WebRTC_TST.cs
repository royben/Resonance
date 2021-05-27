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
            if (IsRunningOnAzurePipelines) return;

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

            signal1.Connect();
            signal2.Connect();

            WebRTCAdapter adapter1 = new WebRTCAdapter(signal1, WebRTCAdapterRole.Accept);

            Task.Factory.StartNew(() =>
            {
                adapter1.Connect();
            });

            Thread.Sleep(100);

            WebRTCAdapter adapter2 = new WebRTCAdapter(signal2, WebRTCAdapterRole.Connect);
            adapter2.Connect();

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
                t2.SendResponseAsync(new CalculateResponse() { Sum = 15 }, e.Message.Token);
            };

            t1.Connect();
            t2.Connect();

            var response = t1.SendRequest<CalculateRequest, CalculateResponse>(new CalculateRequest() { A = 10, B = 5 });
            Assert.IsTrue(response.Sum == 15);

            t1.Dispose();
            t2.Dispose();
            signal1.Dispose();
            signal2.Dispose();
        }

        [TestMethod]
        public void Create_Adapter_From_Offer()
        {
            if (IsRunningOnAzurePipelines) return;

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

            signal1.Connect();
            signal2.Connect();

            WebRTCAdapter adapter1 = null;

            signal1.RegisterRequestHandler<WebRTCOfferRequest>(async (_, request) =>
            {
                adapter1 = new WebRTCAdapter(signal1, request.Object, request.Token);
                await adapter1.ConnectAsync();
            });

            WebRTCAdapter adapter2 = new WebRTCAdapter(signal2, WebRTCAdapterRole.Connect);
            adapter2.Connect();

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
                t2.SendResponseAsync(new CalculateResponse() { Sum = 15 }, e.Message.Token);
            };

            t1.Connect();
            t2.Connect();

            var response = t1.SendRequest<CalculateRequest, CalculateResponse>(new CalculateRequest() { A = 10, B = 5 });
            Assert.IsTrue(response.Sum == 15);

            t1.Dispose();
            t2.Dispose();
            signal1.Dispose();
            signal2.Dispose();
        }

        [TestMethod]
        public void Message_Larger_Than_16_KB_Splits()
        {
            if (IsRunningOnAzurePipelines) return;

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

            signal1.Connect();
            signal2.Connect();

            WebRTCAdapter adapter1 = null;

            signal1.RegisterRequestHandler<WebRTCOfferRequest>(async (_, request) =>
            {
                adapter1 = new WebRTCAdapter(signal1, request.Object, request.Token);
                await adapter1.ConnectAsync();
            });

            WebRTCAdapter adapter2 = new WebRTCAdapter(signal2, WebRTCAdapterRole.Connect);
            adapter2.Connect();

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
                t2.SendResponseAsync(new LargeMessageResponse() { Data = (e.Message.Object as LargeMessageRequest).Data }, e.Message.Token);
            };

            t1.Connect();
            t2.Connect();

            byte[] data = TestHelper.GetRandomByteArray(200);

            var response = t1.SendRequest<LargeMessageRequest, LargeMessageResponse>(new LargeMessageRequest() { Data = data });

            Assert.IsTrue(data.SequenceEqual(response.Data));

            t1.Dispose();
            t2.Dispose();
            signal1.Dispose();
            signal2.Dispose();
        }

        [TestMethod]
        public void Stress_Test_With_Large_Data_Volume()
        {
            if (IsRunningOnAzurePipelines) return;

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

            signal1.Connect();
            signal2.Connect();

            WebRTCAdapter adapter1 = null;

            signal1.RegisterRequestHandler<WebRTCOfferRequest>(async (_, request) =>
            {
                adapter1 = new WebRTCAdapter(signal1, request.Object, request.Token);
                await adapter1.ConnectAsync();
            });

            WebRTCAdapter adapter2 = new WebRTCAdapter(signal2, WebRTCAdapterRole.Connect);
            adapter2.Connect();

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
                t2.SendResponseAsync(new LargeMessageResponse() { Data = (e.Message.Object as LargeMessageRequest).Data }, e.Message.Token);
            };

            t1.Connect();
            t2.Connect();

            t1.DefaultRequestTimeout = TimeSpan.FromSeconds(60);

            for (int i = 0; i < 100; i++)
            {
                byte[] data = TestHelper.GetRandomByteArray(60);
                var response = t1.SendRequest<LargeMessageRequest, LargeMessageResponse>(new LargeMessageRequest() { Data = data });
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
            if (IsRunningOnAzurePipelines) return;

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

            signal1.Connect();
            signal2.Connect();

            WebRTCAdapter adapter1 = null;

            signal1.RegisterRequestHandler<WebRTCOfferRequest>(async (_, request) =>
            {
                adapter1 = new WebRTCAdapter(signal1, request.Object, request.Token);
                await adapter1.ConnectAsync();
            });

            WebRTCAdapter adapter2 = new WebRTCAdapter(signal2, WebRTCAdapterRole.Connect);
            adapter2.Connect();

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
                t2.SendResponseAsync(new LargeMessageResponse() { Data = (e.Message.Object as LargeMessageRequest).Data }, e.Message.Token);
            };

            t1.Connect();
            t2.Connect();

            t1.DefaultRequestTimeout = TimeSpan.FromSeconds(60);

            for (int i = 0; i < 100; i++)
            {
                byte[] data = TestHelper.GetRandomByteArray(2);
                var response = t1.SendRequest<LargeMessageRequest, LargeMessageResponse>(new LargeMessageRequest() { Data = data });
                Assert.IsTrue(data.SequenceEqual(response.Data));
            }

            t1.Dispose();
            t2.Dispose();
            signal1.Dispose();
            signal2.Dispose();
        }

        [TestMethod]
        public void Connection_No_Response_Throws_Exception()
        {
            if (IsRunningOnAzurePipelines) return;

            IResonanceTransporter signal2 = ResonanceTransporter.Builder
                .Create()
                .WithInMemoryAdapter()
                .WithAddress("TST")
                .WithJsonTranscoding()
                .Build();

            signal2.Connect();

            Thread.Sleep(100);

            WebRTCAdapter adapter2 = new WebRTCAdapter(signal2, WebRTCAdapterRole.Connect);

            Assert.ThrowsException<ResonanceWebRTCConnectionFailedException>(() =>
            {
                adapter2.Connect();
            });

            adapter2.Dispose();
            signal2.Dispose();
        }
    }
}
