using Microsoft.VisualStudio.TestTools.UnitTesting;
using Resonance.Adapters.InMemory;
using Resonance.Tests.Common;
using Resonance.Tests.Common.Messages;
using Resonance.Transporters;
using System;
using Google.Protobuf;
using Resonance.Protobuf.Transcoding.Protobuf;
using Resonance.Transcoding.Json;
using Resonance.Transcoding.Auto;
using Resonance.Transcoding.Bson;

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
                var type = Type.GetType($"Resonance.Tests.Common.Proto.{typeName}");
                Assert.IsNotNull(type);
                return type;
            }
        }

        [TestMethod]
        public void Bson_Transcoding_With_DateTime_Kind()
        {
            Init();

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

            t1.Connect().Wait();
            t2.Connect().Wait();

            t2.RequestReceived += (s, e) =>
            {
                CalculateRequestWithDate receivedRequest = e.Request.Message as CalculateRequestWithDate;
                t2.SendResponse(new CalculateResponseWithDate() { Sum = receivedRequest.A + receivedRequest.B, Date = receivedRequest.Date }, e.Request.Token);
            };

            var request = new CalculateRequestWithDate() { A = 10, B = 15, Date = DateTime.UtcNow };
            var response = t1.SendRequest<CalculateRequestWithDate, CalculateResponseWithDate>(request).GetAwaiter().GetResult();

            t1.Dispose(true);
            t2.Dispose(true);

            Assert.AreEqual(response.Sum, request.A + request.B);
            Assert.AreEqual(request.Date.Kind, response.Date.Kind);
            Assert.AreEqual(request.Date.ToString(), response.Date.ToString());
        }

        [TestMethod]
        public void Xml_Transcoding()
        {
            Init();

            IResonanceTransporter t1 = ResonanceTransporter.Builder
                .Create()
                .WithInMemoryAdapter()
                .WithAddress("TST")
                .WithXmlTranscoding()
                .Build();

            IResonanceTransporter t2 = ResonanceTransporter.Builder
                .Create()
                .WithInMemoryAdapter()
                .WithAddress("TST")
                .WithXmlTranscoding()
                .Build();

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
        public void Json_Transcoding()
        {
            Init();

            IResonanceTransporter t1 = ResonanceTransporter.Builder
                .Create()
                .WithInMemoryAdapter()
                .WithAddress("TST")
                .WithJsonTranscoding()
                .Build();

            IResonanceTransporter t2 = ResonanceTransporter.Builder
                .Create()
                .WithInMemoryAdapter()
                .WithAddress("TST")
                .WithJsonTranscoding()
                .Build();

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
        public void Protobuf_Transcoding()
        {
            Init();

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

            t1.DefaultRequestTimeout = TimeSpan.FromSeconds(60);

            t1.Connect().Wait();
            t2.Connect().Wait();

            t2.RequestReceived += (s, e) =>
            {
                Common.Proto.CalculateRequest receivedRequest = e.Request.Message as Common.Proto.CalculateRequest;
                t2.SendResponse(new Common.Proto.CalculateResponse() { Sum = receivedRequest.A + receivedRequest.B }, e.Request.Token);
            };

            var request = new Common.Proto.CalculateRequest() { A = 10, B = 15 };
            var response = t1.SendRequest<Common.Proto.CalculateRequest, Common.Proto.CalculateResponse>(request).GetAwaiter().GetResult();

            t1.Dispose(true);
            t2.Dispose(true);

            Assert.AreEqual(response.Sum, request.A + request.B);
        }

        [TestMethod]
        public void Protobuf_Transcoding_Type_Resolver()
        {
            Init();

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

            t1.DefaultRequestTimeout = TimeSpan.FromSeconds(60);

            t1.Connect().Wait();
            t2.Connect().Wait();

            t2.RequestReceived += (s, e) =>
            {
                Common.Proto.CalculateRequest receivedRequest = e.Request.Message as Common.Proto.CalculateRequest;
                t2.SendResponse(new Common.Proto.CalculateResponse() { Sum = receivedRequest.A + receivedRequest.B }, e.Request.Token);
            };

            var request = new Common.Proto.CalculateRequest() { A = 10, B = 15 };
            var response = t1.SendRequest<Common.Proto.CalculateRequest, Common.Proto.CalculateResponse>(request).GetAwaiter().GetResult();

            t1.Dispose(true);
            t2.Dispose(true);

            Assert.AreEqual(response.Sum, request.A + request.B);
        }

        [TestMethod]
        public void Auto_Decoding()
        {
            Init();

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
    }
}
