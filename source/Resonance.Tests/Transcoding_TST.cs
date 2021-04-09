using Microsoft.VisualStudio.TestTools.UnitTesting;
using Resonance.Adapters.InMemory;
using Resonance.Tests.Common;
using Resonance.Messages;
using Resonance.Transporters;
using System;
using Google.Protobuf;
using Resonance.Protobuf.Transcoding.Protobuf;
using Resonance.Transcoding.Json;
using Resonance.Transcoding.Auto;
using Resonance.Transcoding.Bson;
using System.IO;
using System.Threading.Tasks;
using Resonance.Transcoding.Xml;

namespace Resonance.Tests
{
    [TestClass]
    [TestCategory("Transcoding")]
    public class Transcoding_TST : ResonanceTest
    {
        public class CalculateRequestWithDate : CalculateRequest
        {
            public DateTime Date { get; set; }
        }

        public class CalculateResponseWithDate : CalculateResponse
        {
            public DateTime Date { get; set; }
        }

        private class ProtobufTypeResolver : IProtobufMessageTypeResolver
        {
            public Type GetProtobufMessageType(string typeName)
            {
                var type = typeof(CalculateRequest).Assembly.GetType($"Resonance.Messages.Proto.{typeName}");
                Assert.IsNotNull(type);
                return type;
            }
        }





        [TestMethod]
        public void Default_Header_Transcoding()
        {
            ResonanceEncodingInformation encodeInfo = new ResonanceEncodingInformation();
            encodeInfo.Completed = true;
            encodeInfo.ErrorMessage = "Test";
            encodeInfo.HasError = true;
            encodeInfo.IsCompressed = true;
            encodeInfo.Token = Guid.NewGuid().ToString();
            encodeInfo.Transcoding = "test";
            encodeInfo.Type = ResonanceTranscodingInformationType.Response;

            ResonanceDefaultHeaderTranscoder transcoder = new ResonanceDefaultHeaderTranscoder();

            using (MemoryStream mWrite = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(mWrite))
                {
                    transcoder.Encode(writer, encodeInfo);
                }

                ResonanceDecodingInformation decodeInfo = new ResonanceDecodingInformation();

                using (MemoryStream mRead = new MemoryStream(mWrite.ToArray()))
                {
                    using (BinaryReader reader = new BinaryReader(mRead))
                    {
                        transcoder.Decode(reader, decodeInfo);
                    }
                }

                Assert.AreEqual(encodeInfo.Completed, decodeInfo.Completed);
                Assert.AreEqual(encodeInfo.ErrorMessage, decodeInfo.ErrorMessage);
                Assert.AreEqual(encodeInfo.HasError, decodeInfo.HasError);
                Assert.AreEqual(encodeInfo.IsCompressed, decodeInfo.IsCompressed);
                Assert.AreEqual(encodeInfo.Token, decodeInfo.Token);
                Assert.AreEqual(encodeInfo.Transcoding, decodeInfo.Transcoding);
                Assert.AreEqual(encodeInfo.Type, decodeInfo.Type);
            }
        }

        [TestMethod]
        public async Task Bson_Transcoding_With_DateTime_Kind()
        {
            Init();

            if (IsRunningOnAzurePipelines) return; //Hangs when running in a sequence of tests for some reason.

            IResonanceTransporter t1 = ResonanceTransporter.Builder
                .Create()
                .WithInMemoryAdapter()
                .WithAddress("TST")
                .WithBsonTranscoding()
                .Build();

            IResonanceTransporter t2 = ResonanceTransporter.Builder
                .Create()
                .WithInMemoryAdapter()
                .WithAddress("TST")
                .WithBsonTranscoding()
                .Build();

            await t1.Connect();
            await t2.Connect();

            t2.RequestReceived += async (s, e) =>
            {
                CalculateRequestWithDate receivedRequest = e.Request.Message as CalculateRequestWithDate;
                await t2.SendResponse(new CalculateResponseWithDate() { Sum = receivedRequest.A + receivedRequest.B, Date = receivedRequest.Date }, e.Request.Token);
            };


            for (int i = 0; i < 1000; i++)
            {
                var request = new CalculateRequestWithDate() { A = 10, B = 15, Date = DateTime.UtcNow };
                var response = await t1.SendRequest<CalculateRequestWithDate, CalculateResponseWithDate>(request);

                Assert.AreEqual(response.Sum, request.A + request.B);
                Assert.AreEqual(request.Date.Kind, response.Date.Kind);
                Assert.AreEqual(request.Date.ToString(), response.Date.ToString());
            }

            await t1.DisposeAsync(true);
            await t2.DisposeAsync(true);
        }

        [TestMethod]
        public async Task Xml_Transcoding()
        {
            Init();
            await TestUtils.Read_Write_Test(this, new XmlEncoder(), new XmlDecoder(), false, false, 1, 0);
        }

        [TestMethod]
        public async Task Json_Transcoding()
        {
            Init();
            await TestUtils.Read_Write_Test(this, new JsonEncoder(), new JsonDecoder(), false, false, 1, 0);
        }

        [TestMethod]
        public async Task Protobuf_Transcoding()
        {
            Init();

            if (IsRunningOnAzurePipelines) return;

            IResonanceTransporter t1 = ResonanceTransporter.Builder
                .Create()
                .WithInMemoryAdapter()
                .WithAddress("TST")
                .WithProtobufTranscoding()
                .Build();

            IResonanceTransporter t2 = ResonanceTransporter.Builder
                .Create()
                .WithInMemoryAdapter()
                .WithAddress("TST")
                .WithProtobufTranscoding()
                .Build();

            t1.DefaultRequestTimeout = TimeSpan.FromSeconds(10);

            await t1.Connect();
            await t2.Connect();

            t2.RequestReceived += async (s, e) =>
            {
                Messages.Proto.CalculateRequest receivedRequest = e.Request.Message as Messages.Proto.CalculateRequest;
                await t2.SendResponse(new Messages.Proto.CalculateResponse() { Sum = receivedRequest.A + receivedRequest.B }, e.Request.Token);
            };

            var request = new Messages.Proto.CalculateRequest() { A = 10, B = 15 };
            var response = await t1.SendRequest<Messages.Proto.CalculateRequest, Messages.Proto.CalculateResponse>(request);

            await t1.DisposeAsync(true);
            await t2.DisposeAsync(true);

            Assert.AreEqual(response.Sum, request.A + request.B);
        }

        [TestMethod]
        public async Task Protobuf_Transcoding_Type_Resolver()
        {
            Init();

            if (IsRunningOnAzurePipelines) return;

            IResonanceTransporter t1 = ResonanceTransporter.Builder
                .Create()
                .WithInMemoryAdapter()
                .WithAddress("TST")
                .WithProtobufTranscoding()
                .WithMessageTypeHeaderMethod(MessageTypeHeaderMethod.Name)
                .WithTypeResolver<ProtobufTypeResolver>()
                .Build();

            IResonanceTransporter t2 = ResonanceTransporter.Builder
                .Create()
                .WithInMemoryAdapter()
                .WithAddress("TST")
                .WithProtobufTranscoding()
                .WithMessageTypeHeaderMethod(MessageTypeHeaderMethod.Name)
                .WithTypeResolver<ProtobufTypeResolver>()
                .Build();

            await t1.Connect();
            await t2.Connect();

            t2.RequestReceived += async (s, e) =>
            {
                Messages.Proto.CalculateRequest receivedRequest = e.Request.Message as Messages.Proto.CalculateRequest;
                await t2.SendResponse(new Messages.Proto.CalculateResponse() { Sum = receivedRequest.A + receivedRequest.B }, e.Request.Token);
            };

            var request = new Messages.Proto.CalculateRequest() { A = 10, B = 15 };
            var response = await t1.SendRequest<Messages.Proto.CalculateRequest, Messages.Proto.CalculateResponse>(request);

            await t1.DisposeAsync(true);
            await t2.DisposeAsync(true);

            Assert.AreEqual(response.Sum, request.A + request.B);
        }

        [TestMethod]
        public async Task Auto_Decoding__Needs_A_Second_Run()
        {
            return;
            //This test needs a second run. not sure why.
            Init();

            if (IsRunningOnAzurePipelines) return; //Hangs when running in a sequence of tests for some reason.

            IResonanceTransporter t1 = ResonanceTransporter.Builder
                .Create()
                .WithInMemoryAdapter()
                .WithAddress("TST")
                .WithTranscoding<BsonEncoder, AutoDecoder>()
                .Build();

            IResonanceTransporter t2 = ResonanceTransporter.Builder
                .Create()
                .WithInMemoryAdapter()
                .WithAddress("TST")
                .WithTranscoding<JsonEncoder, AutoDecoder>()
                .Build();


            await t1.Connect();
            await t2.Connect();

            t2.RequestReceived += async (s, e) =>
            {
                CalculateRequest receivedRequest = e.Request.Message as CalculateRequest;
                await t2.SendResponse(new CalculateResponse() { Sum = receivedRequest.A + receivedRequest.B }, e.Request.Token);
            };

            var request = new CalculateRequest() { A = 10, B = 15 };
            var response = await t1.SendRequest<CalculateRequest, CalculateResponse>(request, new ResonanceRequestConfig()
            {
                Timeout = TimeSpan.FromSeconds(20)
            });

            await t1.DisposeAsync(true);
            await t2.DisposeAsync(true);

            Assert.AreEqual(response.Sum, request.A + request.B);
        }
    }
}
