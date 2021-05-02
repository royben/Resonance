using Resonance;
using Resonance.Compressors;
using System;
using System.Reflection;
using static Resonance.ResonanceTransporterBuilder;

public static class ResonanceTransporterBuilderExtension
{
    /// <summary>
    /// Sets the transporter encoding/decoding to compress/decompress data using the fast LZ4 compression algorithm.
    /// </summary>
    /// <param name="compressionBuilder">The compression builder.</param>
    public static IBuildTransporter WithLZ4Compression(this ICompressionBuilder compressionBuilder)
    {
        IResonanceTransporter transporter = compressionBuilder.GetType().GetProperty("Transporter", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(compressionBuilder) as IResonanceTransporter;
        transporter.Encoder.CompressionConfiguration.Compressor = new LZ4Compressor();
        transporter.Decoder.CompressionConfiguration.Compressor = new LZ4Compressor();
        transporter.Encoder.CompressionConfiguration.Enabled = true;
        transporter.Decoder.CompressionConfiguration.Enabled = true;
        return compressionBuilder as ResonanceTransporterBuilder;
    }
}