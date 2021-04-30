using Resonance;
using Resonance.MessagePack.Transcoding.MessagePack;
using System;
using System.Reflection;
using static Resonance.ResonanceTransporterBuilder;

public static class ResonanceTransporterBuilderExtension
{
    /// <summary>
    /// Sets the transporter encoding/decoding to MessagePack.
    /// </summary>
    /// <param name="adapterBuilder">The adapter builder.</param>
    public static IKeepAliveBuilder WithMessagePackTranscoding(this ITranscodingBuilder adapterBuilder)
    {
        adapterBuilder.WithTranscoding<MessagePackEncoder, MessagePackDecoder>();
        return adapterBuilder as ResonanceTransporterBuilder;
    }
}