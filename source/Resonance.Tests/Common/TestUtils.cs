using Microsoft.VisualStudio.TestTools.UnitTesting;
using Resonance.Adapters.InMemory;
using Resonance.Messages;
using Resonance.Transcoding.Json;
using Resonance.Transporters;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.Tests.Common
{
    public static class TestUtils
    {
        public static void Read_Write_Test(ResonanceTest test, IResonanceEncoder encoder, IResonanceDecoder decoder, bool enableCryptography, int count, int maxOutliersPercentage)
        {
            Read_Write_Test(test, new InMemoryAdapter("TST"), new InMemoryAdapter("TST"), encoder, decoder , enableCryptography, count, maxOutliersPercentage);
        }

        public static void Read_Write_Test(ResonanceTest test, IResonanceAdapter adapter1, IResonanceAdapter adapter2, bool enableCryptography, int count, int maxOutliersPercentage)
        {
            Read_Write_Test(test, adapter1, adapter2, new JsonEncoder(), new JsonDecoder(), enableCryptography, count, maxOutliersPercentage);
        }

        public static void Read_Write_Test(ResonanceTest test, IResonanceAdapter adapter1, IResonanceAdapter adapter2, IResonanceEncoder encoder, IResonanceDecoder decoder, bool enableCryptography, int count, int maxOutliersPercentage)
        {
            IResonanceTransporter t1 = ResonanceTransporter.Builder
                .Create()
                .WithAdapter(adapter1)
                .WithTranscoding(encoder, decoder)
                .NoKeepAlive()
                .Build();

            IResonanceTransporter t2 = ResonanceTransporter.Builder
                .Create()
                .WithAdapter(adapter2)
                .WithTranscoding(encoder, decoder)
                .NoKeepAlive()
                .Build();

            Read_Write_Test(test, t1, t2, enableCryptography, count, maxOutliersPercentage);
        }

        public static void Read_Write_Test(ResonanceTest test, IResonanceTransporter t1, IResonanceTransporter t2, bool enableCryptography, int count, int maxOutliersPercentage)
        {
            t1.CryptographyConfiguration.Enabled = enableCryptography;
            t2.CryptographyConfiguration.Enabled = enableCryptography;

            t2.Connect().GetAwaiter().GetResult();
            t1.Connect().GetAwaiter().GetResult();

            t2.RequestReceived += (s, e) =>
            {
                CalculateRequest receivedRequest = e.Request.Message as CalculateRequest;
                t2.SendResponse(new CalculateResponse() { Sum = receivedRequest.A + receivedRequest.B }, e.Request.Token).GetAwaiter().GetResult();
            };

            Stopwatch watch = new Stopwatch();

            List<double> measurements = new List<double>();

            for (int i = 0; i < count; i++)
            {
                watch.Restart();

                var request = new CalculateRequest() { A = 10, B = i };
                var response = t1.SendRequest<CalculateRequest, CalculateResponse>(request).GetAwaiter().GetResult();

                measurements.Add(watch.ElapsedMilliseconds);

                Assert.AreEqual(response.Sum, request.A + request.B);
            }

            watch.Stop();

            t1.Dispose(true);
            t2.Dispose(true);

            if (count > 1 && maxOutliersPercentage > 0)
            {
                var outliers = TestHelper.GetOutliers(measurements);

                double percentageOfOutliers = outliers.Count / (double)measurements.Count * 100d;

                if (!test.IsRunningOnAzurePipelines)
                {
                    Assert.IsTrue(percentageOfOutliers < maxOutliersPercentage, $"Request/Response duration measurements contains {percentageOfOutliers}% outliers and is considered a performance issue.");
                }
            }
        }
    }
}
