using Microsoft.VisualStudio.TestTools.UnitTesting;
using Resonance.Adapters.InMemory;
using Resonance.Adapters.Tcp;
using Resonance.Adapters.Udp;
using Resonance.Adapters.Usb;
using Resonance.Tests.Common;
using Resonance.Transcoding.Json;
using System;
using System.Net;

namespace Resonance.Tests
{
    [TestClass]
    [TestCategory("Builders")]
    public class Builders : ResonanceTest
    {
        [TestMethod]
        public void Transporter_Builder()
        {
            Init();

            IResonanceTransporter transporter = ResonanceTransporter.Builder
                .Create()
                .WithTcpAdapter()
                .WithAddress("192.168.1.1")
                .WithPort(1111)
                .WithTranscoding<JsonEncoder, JsonDecoder>()
                .WithKeepAlive(TimeSpan.FromSeconds(5), 10)
                .WithEncryption("pass")
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
            Assert.IsTrue(transporter.Encoder.EncryptionConfiguration.Enabled);
            Assert.IsTrue(transporter.Decoder.EncryptionConfiguration.Enabled);
            Assert.IsTrue(transporter.Encoder.CompressionConfiguration.Enabled);
            Assert.IsTrue(transporter.Decoder.CompressionConfiguration.Enabled);

            transporter.CreateBuilder()
                .WithUdpAdapter()
                .WithLocalEndPoint(new IPEndPoint(IPAddress.Parse("192.168.1.1"), 1))
                .WithRemoteEndPoint(new IPEndPoint(IPAddress.Parse("192.168.1.2"), 2))
                .WithTranscoding(new JsonEncoder(), new JsonDecoder())
                .NoKeepAlive()
                .NoEncryption()
                .NoCompression()
                .Build();

            Assert.IsInstanceOfType(transporter.Adapter, typeof(UdpAdapter));
            Assert.IsTrue((transporter.Adapter as UdpAdapter).LocalEndPoint.Address.ToString() == "192.168.1.1");
            Assert.IsTrue((transporter.Adapter as UdpAdapter).LocalEndPoint.Port == 1);
            Assert.IsTrue((transporter.Adapter as UdpAdapter).RemoteEndPoint.Address.ToString() == "192.168.1.2");
            Assert.IsTrue((transporter.Adapter as UdpAdapter).RemoteEndPoint.Port == 2);
            Assert.IsInstanceOfType(transporter.Encoder, typeof(JsonEncoder));
            Assert.IsInstanceOfType(transporter.Decoder, typeof(JsonDecoder));
            Assert.IsFalse(transporter.KeepAliveConfiguration.Enabled);
            Assert.IsFalse(transporter.Encoder.EncryptionConfiguration.Enabled);
            Assert.IsFalse(transporter.Decoder.EncryptionConfiguration.Enabled);
            Assert.IsFalse(transporter.Encoder.CompressionConfiguration.Enabled);
            Assert.IsFalse(transporter.Decoder.CompressionConfiguration.Enabled);

            transporter.CreateBuilder()
                .WithInMemoryAdapter()
                .WithAddress("TST")
                .WithTranscoding(new JsonEncoder(), new JsonDecoder())
                .NoKeepAlive()
                .NoEncryption()
                .NoCompression()
                .Build();

            Assert.IsInstanceOfType(transporter.Adapter, typeof(InMemoryAdapter));
            Assert.IsTrue((transporter.Adapter as InMemoryAdapter).Address == "TST");


            IResonanceTransporter usbTransporter = ResonanceTransporter.Builder
               .Create()
               .WithUsbAdapter()
               .WithPort("COM1")
               .WithBaudRate(Adapters.Usb.BaudRates.BR_115200)
               .WithTranscoding<JsonEncoder, JsonDecoder>()
               .NoKeepAlive()
               .WithEncryption("pass")
               .WithCompression()
               .Build();

            Assert.IsNotNull(usbTransporter);
            Assert.IsInstanceOfType(usbTransporter.Adapter, typeof(UsbAdapter));
            Assert.IsTrue((usbTransporter.Adapter as UsbAdapter).Port == "COM1");
            Assert.IsTrue((usbTransporter.Adapter as UsbAdapter).BaudRate == (int)BaudRates.BR_115200);
            Assert.IsInstanceOfType(usbTransporter.Encoder, typeof(JsonEncoder));
            Assert.IsInstanceOfType(usbTransporter.Decoder, typeof(JsonDecoder));
            Assert.IsFalse(transporter.KeepAliveConfiguration.Enabled);
            Assert.IsTrue(usbTransporter.Encoder.EncryptionConfiguration.Enabled);
            Assert.IsTrue(usbTransporter.Decoder.EncryptionConfiguration.Enabled);
            Assert.IsTrue(usbTransporter.Encoder.CompressionConfiguration.Enabled);
            Assert.IsTrue(usbTransporter.Decoder.CompressionConfiguration.Enabled);
        }
    }
}
