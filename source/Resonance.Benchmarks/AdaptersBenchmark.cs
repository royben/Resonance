using BenchmarkDotNet.Attributes;
using Resonance.Adapters.InMemory;
using Resonance.Transcoding.Json;
using Resonance.Adapters.Tcp;
using Resonance.Adapters.Udp;
using Resonance.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Resonance.Benchmarks
{
    [KeepBenchmarkFiles]
    [CsvExporter]
    [CsvMeasurementsExporter]
    [HtmlExporter]
    [PlainExporter]
    [MarkdownExporterAttribute.GitHub]
    //[RPlotExporter]
    public class AdaptersBenchmark
    {
        [Benchmark(Description = "1000 Request/Response Json Encoding")]
        public void Json_Encoding()
        {
            IResonanceTransporter t1 = ResonanceTransporter.Builder.Create()
                .WithInMemoryAdapter().WithAddress("TST")
                .WithJsonTranscoding()
                .Build();
            IResonanceTransporter t2 = ResonanceTransporter.Builder.Create()
                .WithInMemoryAdapter().WithAddress("TST")
                .WithJsonTranscoding()
                .Build();

            t1.ConnectAsync().Wait();
            t2.ConnectAsync().Wait();

            t2.RequestReceived += (s, e) =>
            {
                CalculateRequest receivedRequest = e.Message.Object as CalculateRequest;
                t2.SendResponseAsync(new CalculateResponse() { Sum = receivedRequest.A + receivedRequest.B }, e.Message.Token);
            };

            for (int i = 0; i < 1000; i++)
            {
                var request = new CalculateRequest() { A = 10, B = i };
                var response = t1.SendRequestAsync<CalculateRequest, CalculateResponse>(request).GetAwaiter().GetResult();
            }

            t1.Dispose();
            t2.Dispose();
        }

        [Benchmark(Description = "1000 Request/Response Json Encoding With Compression")]
        public void Json_Encoding_Compressed()
        {
            IResonanceTransporter t1 = ResonanceTransporter.Builder.Create()
                .WithInMemoryAdapter().WithAddress("TST")
                .WithJsonTranscoding()
                .Build();
            IResonanceTransporter t2 = ResonanceTransporter.Builder.Create()
                .WithInMemoryAdapter().WithAddress("TST")
                .WithJsonTranscoding()
                .Build();

            t1.Encoder.CompressionConfiguration.Enabled = true;
            t2.Encoder.CompressionConfiguration.Enabled = true;

            t1.ConnectAsync().Wait();
            t2.ConnectAsync().Wait();

            t2.RequestReceived += (s, e) =>
            {
                CalculateRequest receivedRequest = e.Message.Object as CalculateRequest;
                t2.SendResponseAsync(new CalculateResponse() { Sum = receivedRequest.A + receivedRequest.B }, e.Message.Token);
            };

            for (int i = 0; i < 1000; i++)
            {
                var request = new CalculateRequest() { A = 10, B = i };
                var response = t1.SendRequestAsync<CalculateRequest, CalculateResponse>(request).GetAwaiter().GetResult();
            }

            t1.Dispose();
            t2.Dispose();
        }

        [Benchmark(Description = "1000 Request/Response Json Encoding With Encryption")]
        public void Json_Encoding_Encrypted()
        {
            IResonanceTransporter t1 = ResonanceTransporter.Builder.Create()
                .WithInMemoryAdapter().WithAddress("TST")
                .WithJsonTranscoding()
                .Build();
            IResonanceTransporter t2 = ResonanceTransporter.Builder.Create()
                .WithInMemoryAdapter().WithAddress("TST")
                .WithJsonTranscoding()
                .Build();

            t1.CryptographyConfiguration.Enabled = true;
            t2.CryptographyConfiguration.Enabled = true;

            t1.ConnectAsync().Wait();
            t2.ConnectAsync().Wait();

            t2.RequestReceived += (s, e) =>
            {
                CalculateRequest receivedRequest = e.Message.Object as CalculateRequest;
                t2.SendResponseAsync(new CalculateResponse() { Sum = receivedRequest.A + receivedRequest.B }, e.Message.Token);
            };

            for (int i = 0; i < 1000; i++)
            {
                var request = new CalculateRequest() { A = 10, B = i };
                var response = t1.SendRequestAsync<CalculateRequest, CalculateResponse>(request).GetAwaiter().GetResult();
            }

            t1.Dispose();
            t2.Dispose();
        }

        [Benchmark(Description = "1000 Request/Response Json Encoding With Compression & Encryption")]
        public void Json_Encoding_Compressed_Encrypted()
        {
            IResonanceTransporter t1 = ResonanceTransporter.Builder.Create()
                .WithInMemoryAdapter().WithAddress("TST")
                .WithJsonTranscoding()
                .Build();
            IResonanceTransporter t2 = ResonanceTransporter.Builder.Create()
                .WithInMemoryAdapter().WithAddress("TST")
                .WithJsonTranscoding()
                .Build();

            t1.CryptographyConfiguration.Enabled = true;
            t2.CryptographyConfiguration.Enabled = true;
            t1.Encoder.CompressionConfiguration.Enabled = true;
            t2.Encoder.CompressionConfiguration.Enabled = true;

            t1.ConnectAsync().Wait();
            t2.ConnectAsync().Wait();

            t2.RequestReceived += (s, e) =>
            {
                CalculateRequest receivedRequest = e.Message.Object as CalculateRequest;
                t2.SendResponseAsync(new CalculateResponse() { Sum = receivedRequest.A + receivedRequest.B }, e.Message.Token);
            };

            for (int i = 0; i < 1000; i++)
            {
                var request = new CalculateRequest() { A = 10, B = i };
                var response = t1.SendRequestAsync<CalculateRequest, CalculateResponse>(request).GetAwaiter().GetResult();
            }

            t1.Dispose();
            t2.Dispose();
        }
    }
}
