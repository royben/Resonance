﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Resonance.Adapters.InMemory;
using Resonance.Tests.Common;
using Resonance.Messages;
using System;
using Google.Protobuf;
using Resonance.Protobuf.Transcoding.Protobuf;
using Resonance.Transcoding.Json;
using Resonance.Transcoding.Auto;
using Resonance.Transcoding.Bson;
using System.IO;
using System.Threading.Tasks;
using Resonance.Transcoding.Xml;
using Resonance.MessagePack.Transcoding.MessagePack;
using Resonance.RPC;

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
            encodeInfo.RPCSignature = RPCSignature.FromString("Method:Service.MyMethod");
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
                Assert.AreEqual(encodeInfo.RPCSignature.ToString(), "Method:Service.MyMethod");
            }
        }

        [TestMethod]
        public void Bson_Transcoding_With_DateTime_Kind()
        {


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

            t1.Connect();
            t2.Connect();

            t2.RequestReceived += (s, e) =>
            {
                CalculateRequestWithDate receivedRequest = e.Message.Object as CalculateRequestWithDate;
                t2.SendResponse(new CalculateResponseWithDate() { Sum = receivedRequest.A + receivedRequest.B, Date = receivedRequest.Date }, e.Message.Token);
            };


            for (int i = 0; i < 1000; i++)
            {
                var request = new CalculateRequestWithDate() { A = 10, B = 15, Date = DateTime.UtcNow };
                var response = t1.SendRequest<CalculateRequestWithDate, CalculateResponseWithDate>(request);

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

            TestUtils.Read_Write_Test(this, new XmlEncoder(), new XmlDecoder(), false, 1, 0);
        }

        [TestMethod]
        public void Json_Transcoding()
        {
            TestUtils.Read_Write_Test(this, new JsonEncoder(), new JsonDecoder(), false, 1 , 0);
        }

        [TestMethod]
        public void MessagePack_Transcoding()
        {
            TestUtils.Read_Write_Test(this, new MessagePackEncoder(), new MessagePackDecoder(), false, 1, 0);
        }

        [TestMethod]
        public void Protobuf_Transcoding()
        {


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

            t1.Connect();
            t2.Connect();

            t2.RequestReceived += (s, e) =>
            {
                Messages.Proto.CalculateRequest receivedRequest = e.Message.Object as Messages.Proto.CalculateRequest;
                t2.SendResponse(new Messages.Proto.CalculateResponse() { Sum = receivedRequest.A + receivedRequest.B }, e.Message.Token);
            };

            var request = new Messages.Proto.CalculateRequest() { A = 10, B = 15 };
            var response = t1.SendRequest<Messages.Proto.CalculateRequest, Messages.Proto.CalculateResponse>(request);

            t1.Dispose(true);
            t2.Dispose(true);

            Assert.AreEqual(response.Sum, request.A + request.B);
        }

        [TestMethod]
        public void Protobuf_Transcoding_Type_Resolver()
        {


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

            t1.Connect();
            t2.Connect();

            t2.RequestReceived += (s, e) =>
            {
                Messages.Proto.CalculateRequest receivedRequest = e.Message.Object as Messages.Proto.CalculateRequest;
                t2.SendResponse(new Messages.Proto.CalculateResponse() { Sum = receivedRequest.A + receivedRequest.B }, e.Message.Token);
            };

            var request = new Messages.Proto.CalculateRequest() { A = 10, B = 15 };
            var response = t1.SendRequest<Messages.Proto.CalculateRequest, Messages.Proto.CalculateResponse>(request);

            t1.Dispose(true);
            t2.Dispose(true);

            Assert.AreEqual(response.Sum, request.A + request.B);
        }

        [TestMethod]
        public void Auto_Decoding__Needs_A_Second_Run()
        {
            return;
            //This test needs a second run. not sure why.


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


            t1.Connect();
            t2.Connect();

            t2.RequestReceived += (s, e) =>
            {
                CalculateRequest receivedRequest = e.Message.Object as CalculateRequest;
                t2.SendResponse(new CalculateResponse() { Sum = receivedRequest.A + receivedRequest.B }, e.Message.Token);
            };

            var request = new CalculateRequest() { A = 10, B = 15 };
            var response = t1.SendRequest<CalculateRequest, CalculateResponse>(request, new ResonanceRequestConfig()
            {
                Timeout = TimeSpan.FromSeconds(20)
            });

            t1.Dispose(true);
            t2.Dispose(true);

            Assert.AreEqual(response.Sum, request.A + request.B);
        }
    }
}
