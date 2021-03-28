using BenchmarkDotNet.Attributes;
using Resonance.Benchmarks.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.Benchmarks
{
    [KeepBenchmarkFiles]
    [CsvExporter]
    [CsvMeasurementsExporter]
    [HtmlExporter]
    [PlainExporter]
    [MarkdownExporterAttribute.GitHub]
    public class TranscodingBenchmark
    {
        [Benchmark(Description = "1000 Request/Response Json Transcoding")]
        public void Json_Encoding()
        {
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

            for (int i = 0; i < 1000; i++)
            {
                var request = new CalculateRequest() { A = 10, B = i };
                var response = t1.SendRequest<CalculateRequest, CalculateResponse>(request).GetAwaiter().GetResult();
            }

            t1.Dispose(true);
            t2.Dispose(true);
        }

        [Benchmark(Description = "1000 Request/Response Bson Transcoding")]
        public void Bson_Encoding()
        {
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
                CalculateRequest receivedRequest = e.Request.Message as CalculateRequest;
                t2.SendResponse(new CalculateResponse() { Sum = receivedRequest.A + receivedRequest.B }, e.Request.Token);
            };

            for (int i = 0; i < 1000; i++)
            {
                var request = new CalculateRequest() { A = 10, B = i };
                var response = t1.SendRequest<CalculateRequest, CalculateResponse>(request).GetAwaiter().GetResult();
            }

            t1.Dispose(true);
            t2.Dispose(true);
        }

        [Benchmark(Description = "1000 Request/Response Protobuf Transcoding")]
        public void Protobuf_Encoding()
        {
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

            t1.Connect().Wait();
            t2.Connect().Wait();

            t2.RequestReceived += (s, e) =>
            {
                Messages.Proto.CalculateRequest receivedRequest = e.Request.Message as Messages.Proto.CalculateRequest;
                t2.SendResponse(new Messages.Proto.CalculateResponse() { Sum = receivedRequest.A + receivedRequest.B }, e.Request.Token);
            };

            for (int i = 0; i < 1000; i++)
            {
                var request = new Messages.Proto.CalculateRequest() { A = 10, B = i };
                var response = t1.SendRequest<Messages.Proto.CalculateRequest, Messages.Proto.CalculateResponse>(request).GetAwaiter().GetResult();
            }

            t1.Dispose(true);
            t2.Dispose(true);
        }
    }
}