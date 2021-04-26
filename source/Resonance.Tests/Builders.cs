using Microsoft.VisualStudio.TestTools.UnitTesting;
using Resonance.Adapters.InMemory;
using Resonance.Adapters.SharedMemory;
using Resonance.Adapters.SignalR;
using Resonance.Adapters.Tcp;
using Resonance.Adapters.Udp;
using Resonance.Adapters.Usb;
using Resonance.Adapters.WebRTC;
using Resonance.MessagePack.Transcoding.MessagePack;
using Resonance.Protobuf.Transcoding.Protobuf;
using Resonance.SignalR;
using Resonance.Tests.Common;
using Resonance.Tests.SignalR;
using Resonance.Transcoding.Json;
using Resonance.WebRTC.Messages;
using System;
using System.Net;

namespace Resonance.Tests
{
    [TestClass]
    [TestCategory("Builders")]
    public class Builders : ResonanceTest
    {
        [TestMethod]
        public void Transporter_Builder_With_Tcp_Adapter()
        {
            IResonanceTransporter transporter = ResonanceTransporter.Builder
                .Create()
                .WithTcpAdapter()
                .WithAddress("192.168.1.1")
                .WithPort(1111)
                .WithTranscoding<JsonEncoder, JsonDecoder>()
                .WithKeepAlive(TimeSpan.FromSeconds(5), 10)
                .WithEncryption()
                .WithCompression()
                .Build();

            Assert.IsNotNull(transporter);
            Assert.IsInstanceOfType(transporter.Adapter, typeof(TcpAdapter));
            Assert.IsTrue((transporter.Adapter as TcpAdapter).Address == "192.168.1.1");
            Assert.IsTrue((transporter.Adapter as TcpAdapter).Port == 1111);
            Assert.IsInstanceOfType(transporter.Encoder, typeof(JsonEncoder));
            Assert.IsInstanceOfType(transporter.Decoder, typeof(JsonDecoder));
            Assert.IsTrue(transporter.KeepAliveConfiguration.Enabled);
            Assert.IsTrue(transporter.KeepAliveConfiguration.Interval == TimeSpan.FromSeconds(5));
            Assert.IsTrue(transporter.KeepAliveConfiguration.Retries == 10);
            Assert.IsTrue(transporter.CryptographyConfiguration.Enabled);
            Assert.IsTrue(transporter.Encoder.CompressionConfiguration.Enabled);
            Assert.IsTrue(transporter.Decoder.CompressionConfiguration.Enabled);
        }

        [TestMethod]
        public void Transporter_Builder_With_Shared_Memory_Adapter()
        {
            IResonanceTransporter transporter = ResonanceTransporter.Builder
                .Create()
                .WithSharedMemoryAdapter()
                .WithAddress("TEST")
                .WithTranscoding<JsonEncoder, JsonDecoder>()
                .NoKeepAlive()
                .WithEncryption()
                .WithCompression()
                .Build();

            Assert.IsNotNull(transporter);
            Assert.IsInstanceOfType(transporter.Adapter, typeof(SharedMemoryAdapter));
            Assert.IsTrue((transporter.Adapter as SharedMemoryAdapter).Address == "TEST");
            Assert.IsInstanceOfType(transporter.Encoder, typeof(JsonEncoder));
            Assert.IsInstanceOfType(transporter.Decoder, typeof(JsonDecoder));
            Assert.IsFalse(transporter.KeepAliveConfiguration.Enabled);
            Assert.IsTrue(transporter.CryptographyConfiguration.Enabled);
            Assert.IsTrue(transporter.Encoder.CompressionConfiguration.Enabled);
            Assert.IsTrue(transporter.Decoder.CompressionConfiguration.Enabled);
        }

        [TestMethod]
        public void Transporter_Builder_With_InMemory_Adapter()
        {
            IResonanceTransporter transporter = ResonanceTransporter.Builder
                .Create()
                .WithInMemoryAdapter()
                .WithAddress("TST")
                .WithTranscoding(new JsonEncoder(), new JsonDecoder())
                .NoKeepAlive()
                .NoEncryption()
                .NoCompression()
                .Build();

            Assert.IsInstanceOfType(transporter.Adapter, typeof(InMemoryAdapter));
            Assert.IsTrue((transporter.Adapter as InMemoryAdapter).Address == "TST");
        }

        [TestMethod]
        public void Transporter_Builder_With_Usb_Adapter()
        {
            IResonanceTransporter usbTransporter = ResonanceTransporter.Builder
               .Create()
               .WithUsbAdapter()
               .WithPort("COM1")
               .WithBaudRate(Adapters.Usb.BaudRates.BR_115200)
               .WithTranscoding<JsonEncoder, JsonDecoder>()
               .NoKeepAlive()
               .WithEncryption()
               .WithCompression()
               .Build();

            Assert.IsNotNull(usbTransporter);
            Assert.IsInstanceOfType(usbTransporter.Adapter, typeof(UsbAdapter));
            Assert.IsTrue((usbTransporter.Adapter as UsbAdapter).Port == "COM1");
            Assert.IsTrue((usbTransporter.Adapter as UsbAdapter).BaudRate == (int)BaudRates.BR_115200);
            Assert.IsInstanceOfType(usbTransporter.Encoder, typeof(JsonEncoder));
            Assert.IsInstanceOfType(usbTransporter.Decoder, typeof(JsonDecoder));
            Assert.IsFalse(usbTransporter.KeepAliveConfiguration.Enabled);
            Assert.IsTrue(usbTransporter.CryptographyConfiguration.Enabled);
            Assert.IsTrue(usbTransporter.Encoder.CompressionConfiguration.Enabled);
            Assert.IsTrue(usbTransporter.Decoder.CompressionConfiguration.Enabled);
        }

        [TestMethod]
        public void Transporter_Builder_With_SignalR_Adapter()
        {
            IResonanceTransporter signalRTransporter = ResonanceTransporter.Builder
               .Create()
               .WithSignalRAdapter(SignalRMode.Legacy)
               .WithCredentials<TestCredentials>(new TestCredentials() { Name = "TEST" })
               .WithServiceId("1234")
               .WithUrl("some url")
               .WithTranscoding<JsonEncoder, JsonDecoder>()
               .NoKeepAlive()
               .WithEncryption()
               .WithCompression()
               .Build();

            Assert.IsNotNull(signalRTransporter);
            Assert.IsInstanceOfType(signalRTransporter.Adapter, typeof(SignalRAdapter<TestCredentials>));
            Assert.IsTrue((signalRTransporter.Adapter as SignalRAdapter<TestCredentials>).Mode == SignalRMode.Legacy);
            Assert.IsTrue((signalRTransporter.Adapter as SignalRAdapter<TestCredentials>).Credentials.Name == "TEST");
            Assert.IsTrue((signalRTransporter.Adapter as SignalRAdapter<TestCredentials>).ServiceId == "1234");
            Assert.IsTrue((signalRTransporter.Adapter as SignalRAdapter<TestCredentials>).Url == "some url");
            Assert.IsInstanceOfType(signalRTransporter.Encoder, typeof(JsonEncoder));
            Assert.IsInstanceOfType(signalRTransporter.Decoder, typeof(JsonDecoder));
            Assert.IsFalse(signalRTransporter.KeepAliveConfiguration.Enabled);
            Assert.IsTrue(signalRTransporter.CryptographyConfiguration.Enabled);
            Assert.IsTrue(signalRTransporter.Encoder.CompressionConfiguration.Enabled);
            Assert.IsTrue(signalRTransporter.Decoder.CompressionConfiguration.Enabled);
        }

        [TestMethod]
        public void Transporter_Builder_With_WebRTC_Adapter()
        {
            IResonanceTransporter signaling = new ResonanceTransporter();

            IResonanceTransporter webRtcAdapter = ResonanceTransporter.Builder
                .Create()
                .WithWebRTCAdapter()
                .WithSignalingTransporter(signaling)
                .WithRole(WebRTCAdapterRole.Connect)
                .WithIceServer("some ice server")
                .WithTranscoding<JsonEncoder, JsonDecoder>()
                .NoKeepAlive()
                .WithEncryption()
                .WithCompression()
                .Build();

            Assert.IsNotNull(webRtcAdapter);
            Assert.IsInstanceOfType(webRtcAdapter.Adapter, typeof(WebRTCAdapter));
            Assert.IsTrue((webRtcAdapter.Adapter as WebRTCAdapter).IceServers[0].Url == "some ice server");
            Assert.IsTrue((webRtcAdapter.Adapter as WebRTCAdapter).Role == WebRTCAdapterRole.Connect);
            Assert.IsTrue((webRtcAdapter.Adapter as WebRTCAdapter).SignalingTransporter == signaling);
            Assert.IsInstanceOfType(webRtcAdapter.Encoder, typeof(JsonEncoder));
            Assert.IsInstanceOfType(webRtcAdapter.Decoder, typeof(JsonDecoder));
            Assert.IsFalse(webRtcAdapter.KeepAliveConfiguration.Enabled);
            Assert.IsTrue(webRtcAdapter.CryptographyConfiguration.Enabled);
            Assert.IsTrue(webRtcAdapter.Encoder.CompressionConfiguration.Enabled);
            Assert.IsTrue(webRtcAdapter.Decoder.CompressionConfiguration.Enabled);

            webRtcAdapter = ResonanceTransporter.Builder
                .Create()
                .WithWebRTCAdapter()
                .WithSignalingTransporter(signaling)
                .WithOfferRequest(new WebRTCOfferRequest(), "token")
                .WithIceServer("some ice server")
                .WithTranscoding<JsonEncoder, JsonDecoder>()
                .NoKeepAlive()
                .WithEncryption()
                .WithCompression()
                .Build();

            Assert.IsNotNull(webRtcAdapter);
            Assert.IsInstanceOfType(webRtcAdapter.Adapter, typeof(WebRTCAdapter));
            Assert.IsTrue((webRtcAdapter.Adapter as WebRTCAdapter).IceServers[0].Url == "some ice server");
            Assert.IsTrue((webRtcAdapter.Adapter as WebRTCAdapter).Role == WebRTCAdapterRole.Accept);
            Assert.IsTrue((webRtcAdapter.Adapter as WebRTCAdapter).InitializedByOffer);
            Assert.IsTrue((webRtcAdapter.Adapter as WebRTCAdapter).SignalingTransporter == signaling);
            Assert.IsInstanceOfType(webRtcAdapter.Encoder, typeof(JsonEncoder));
            Assert.IsInstanceOfType(webRtcAdapter.Decoder, typeof(JsonDecoder));
            Assert.IsFalse(webRtcAdapter.KeepAliveConfiguration.Enabled);
            Assert.IsTrue(webRtcAdapter.CryptographyConfiguration.Enabled);
            Assert.IsTrue(webRtcAdapter.Encoder.CompressionConfiguration.Enabled);
            Assert.IsTrue(webRtcAdapter.Decoder.CompressionConfiguration.Enabled);
        }

        [TestMethod]
        public void Transporter_Builder_With_MessagePack_Transcoding()
        {
            IResonanceTransporter transporter = ResonanceTransporter.Builder
               .Create()
               .WithInMemoryAdapter()
               .WithAddress("TST")
               .WithMessagePackTranscoding()
               .NoKeepAlive()
               .WithEncryption()
               .WithCompression()
               .Build();

            Assert.IsNotNull(transporter);
            Assert.IsInstanceOfType(transporter.Adapter, typeof(InMemoryAdapter));
            Assert.IsInstanceOfType(transporter.Encoder, typeof(MessagePackEncoder));
            Assert.IsInstanceOfType(transporter.Decoder, typeof(MessagePackDecoder));
            Assert.IsFalse(transporter.KeepAliveConfiguration.Enabled);
            Assert.IsTrue(transporter.CryptographyConfiguration.Enabled);
            Assert.IsTrue(transporter.Encoder.CompressionConfiguration.Enabled);
            Assert.IsTrue(transporter.Decoder.CompressionConfiguration.Enabled);
        }

        [TestMethod]
        public void Transporter_Builder_With_Protobuf_Transcoding()
        {
            IResonanceTransporter transporter = ResonanceTransporter.Builder
               .Create()
               .WithInMemoryAdapter()
               .WithAddress("TST")
               .WithProtobufTranscoding()
               .WithMessageTypeHeaderMethod(MessageTypeHeaderMethod.FullName)
               .NoKeepAlive()
               .WithEncryption()
               .WithCompression()
               .Build();

            Assert.IsNotNull(transporter);
            Assert.IsInstanceOfType(transporter.Adapter, typeof(InMemoryAdapter));
            Assert.IsInstanceOfType(transporter.Encoder, typeof(ProtobufEncoder));
            Assert.IsInstanceOfType(transporter.Decoder, typeof(ProtobufDecoder));
            Assert.IsTrue((transporter.Encoder as ProtobufEncoder).MessageTypeHeaderMethod == MessageTypeHeaderMethod.FullName);
            Assert.IsFalse(transporter.KeepAliveConfiguration.Enabled);
            Assert.IsTrue(transporter.CryptographyConfiguration.Enabled);
            Assert.IsTrue(transporter.Encoder.CompressionConfiguration.Enabled);
            Assert.IsTrue(transporter.Decoder.CompressionConfiguration.Enabled);
        }
    }
}
