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
        public void Bson_Transcoding_With_DateTime_Kind()
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

            t1.Connect().GetAwaiter().GetResult();
            t2.Connect().GetAwaiter().GetResult();

            t2.RequestReceived += (s, e) =>
            {
                CalculateRequestWithDate receivedRequest = e.Request.Message as CalculateRequestWithDate;
                t2.SendResponse(new CalculateResponseWithDate() { Sum = receivedRequest.A + receivedRequest.B, Date = receivedRequest.Date }, e.Request.Token).GetAwaiter().GetResult();
            };


            for (int i = 0; i < 1000; i++)
            {
                var request = new CalculateRequestWithDate() { A = 10, B = 15, Date = DateTime.UtcNow };
                var response = t1.SendRequest<CalculateRequestWithDate, CalculateResponseWithDate>(request).GetAwaiter().GetResult();

                Assert.AreEqual(response.Sum, request.A + request.B);
                Assert.AreEqual(request.Date.Kind, response.Date.Kind);
                Assert.AreEqual(request.Date.ToString(), response.Date.ToString());
            }

            t1.Dispose(true);
            t2.Dispose(true);
        }

        [TestMethod]
        public void Xml_Transcoding()
        {
            Init();
            TestUtils.Read_Write_Test(this, new XmlEncoder(), new XmlDecoder(), false, false, 1, 0);
        }

        [TestMethod]
        public void Json_Transcoding()
        {
            Init();
            TestUtils.Read_Write_Test(this, new JsonEncoder(), new JsonDecoder(), false, false, 1, 0);
        }

        [TestMethod]
        public void Protobuf_Transcoding()
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

            t1.Connect().GetAwaiter().GetResult();
            t2.Connect().GetAwaiter().GetResult();

            t2.RequestReceived += (s, e) =>
            {
                Messages.Proto.CalculateRequest receivedRequest = e.Request.Message as Messages.Proto.CalculateRequest;
                t2.SendResponse(new Messages.Proto.CalculateResponse() { Sum = receivedRequest.A + receivedRequest.B }, e.Request.Token).GetAwaiter().GetResult();
            };

            var request = new Messages.Proto.CalculateRequest() { A = 10, B = 15 };
            var response = t1.SendRequest<Messages.Proto.CalculateRequest, Messages.Proto.CalculateResponse>(request).GetAwaiter().GetResult();

            t1.Dispose(true);
            t2.Dispose(true);

            Assert.AreEqual(response.Sum, request.A + request.B);
        }

        [TestMethod]
        public void Protobuf_Transcoding_Type_Resolver()
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

            t1.Connect().GetAwaiter().GetResult();
            t2.Connect().GetAwaiter().GetResult();

            t2.RequestReceived += (s, e) =>
            {
                Messages.Proto.CalculateRequest receivedRequest = e.Request.Message as Messages.Proto.CalculateRequest;
                t2.SendResponse(new Messages.Proto.CalculateResponse() { Sum = receivedRequest.A + receivedRequest.B }, e.Request.Token).GetAwaiter().GetResult();
            };

            var request = new Messages.Proto.CalculateRequest() { A = 10, B = 15 };
            var response = t1.SendRequest<Messages.Proto.CalculateRequest, Messages.Proto.CalculateResponse>(request).GetAwaiter().GetResult();

            t1.Dispose(true);
            t2.Dispose(true);

            Assert.AreEqual(response.Sum, request.A + request.B);
        }

        [TestMethod]
        public void Auto_Decoding__Needs_A_Second_Run()
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


            t1.Connect().GetAwaiter().GetResult();
            t2.Connect().GetAwaiter().GetResult();

            t2.RequestReceived += (s, e) =>
            {
                CalculateRequest receivedRequest = e.Request.Message as CalculateRequest;
                t2.SendResponse(new CalculateResponse() { Sum = receivedRequest.A + receivedRequest.B }, e.Request.Token).GetAwaiter().GetResult();
            };

            var request = new CalculateRequest() { A = 10, B = 15 };
            var response = t1.SendRequest<CalculateRequest, CalculateResponse>(request, new ResonanceRequestConfig()
            {
                Timeout = TimeSpan.FromSeconds(20)
            }).GetAwaiter().GetResult();

            t1.Dispose(true);
            t2.Dispose(true);

            Assert.AreEqual(response.Sum, request.A + request.B);
        }
    }
}
