using BenchmarkDotNet.Attributes;
using Resonance.Adapters.InMemory;
using Resonance.Adapters.Tcp;
using Resonance.Adapters.Udp;
using Resonance.Benchmarks.Messages;
using Resonance.Tcp;
using Resonance.Transporters;
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
            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            ResonanceJsonTransporter t2 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));

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

        [Benchmark(Description = "1000 Request/Response Json Encoding With Compression")]
        public void Json_Encoding_Compressed()
        {
            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            ResonanceJsonTransporter t2 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));

            t1.Encoder.CompressionConfiguration.Enabled = true;
            t2.Encoder.CompressionConfiguration.Enabled = true;

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

        [Benchmark(Description = "1000 Request/Response Json Encoding With Encryption")]
        public void Json_Encoding_Encrypted()
        {
            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            ResonanceJsonTransporter t2 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));

            t1.Encoder.EncryptionConfiguration.Enabled = true;
            t2.Encoder.EncryptionConfiguration.Enabled = true;

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

        [Benchmark(Description = "1000 Request/Response Json Encoding With Compression & Encryption")]
        public void Json_Encoding_Compressed_Encrypted()
        {
            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
            ResonanceJsonTransporter t2 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));

            t1.Encoder.CompressionConfiguration.Enabled = true;
            t2.Encoder.CompressionConfiguration.Enabled = true;

            t1.Encoder.EncryptionConfiguration.Enabled = true;
            t2.Encoder.EncryptionConfiguration.Enabled = true;

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
    }
}
