using Microsoft.VisualStudio.TestTools.UnitTesting;
using Resonance.Adapters.InMemory;
using Resonance.Compressors;
using Resonance.Tests.Common;
using Resonance.Transcoding.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.Tests
{
    [TestClass]
    [TestCategory("LZ4")]
    public class LZ4_TST : ResonanceTest
    {
        [TestMethod]
        public void LZ4_Read_Write_Basic()
        {
            JsonEncoder encoder = new JsonEncoder();
            encoder.CompressionConfiguration.Enabled = true;
            encoder.CompressionConfiguration.Compressor = new LZ4Compressor();

            JsonDecoder decoder = new JsonDecoder();
            decoder.CompressionConfiguration.Enabled = true;
            decoder.CompressionConfiguration.Compressor = new LZ4Compressor();

            TestUtils.Read_Write_Test(
                this,
                new InMemoryAdapter("TST"),
                new InMemoryAdapter("TST"),
                encoder,
                decoder,
                false,
                1000,
                0);
        }

        [TestMethod]
        public void LZ4_Read_Write_Faster_Than_GZip()
        {
            var gzipCompressor = new GZipCompressor();
            var lz4Compressor = new LZ4Compressor();

            double gzipSeconds = 0;
            double lz4Seconds = 0;

            Stopwatch watch = new Stopwatch();
            watch.Start();

            for (int i = 0; i < 1000; i++)
            {
                byte[] data = TestHelper.GetRandomByteArray(10);
                byte[] compressed = gzipCompressor.Compress(data);
                byte[] deflated = gzipCompressor.Decompress(compressed);
                Assert.IsTrue(data.SequenceEqual(deflated));
            }

            watch.Stop();
            gzipSeconds = watch.Elapsed.TotalSeconds;

            watch = new Stopwatch();
            watch.Start();

            for (int i = 0; i < 1000; i++)
            {
                byte[] data = TestHelper.GetRandomByteArray(10);
                byte[] compressed = lz4Compressor.Compress(data);
                byte[] deflated = lz4Compressor.Decompress(compressed);
                Assert.IsTrue(data.SequenceEqual(deflated));
            }

            watch.Stop();
            lz4Seconds = watch.Elapsed.TotalSeconds;

            Assert.IsTrue(lz4Seconds < gzipSeconds);
        }
    }
}
