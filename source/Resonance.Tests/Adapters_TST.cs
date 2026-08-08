using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Resonance.Adapters.Tcp;
using Resonance.Adapters.Udp;
using Resonance.Adapters.Usb;
using Resonance.Tests.Common;
using Resonance.Messages;

using Resonance.Servers.Tcp;
using Resonance.Servers.NamedPipes;
using Resonance.Adapters.NamedPipes;
using Resonance.Adapters.InMemory;
using System.Threading.Tasks;
using Resonance.Adapters.SharedMemory;

namespace Resonance.Tests
{
    [TestClass]
    [TestCategory("Adapters")]
    public class Adapters_TST : ResonanceTest
    {
        [TestMethod]
        public void InMemory_Adapter_Writing_Reading()
        {
            TestUtils.Read_Write_Test(
                this, 
                new InMemoryAdapter("TST"), 
                new InMemoryAdapter("TST"),
                false, 
                1000, 
                2);
        }

        [TestMethod]
        public void Tcp_Adapter_Writing_Reading()
        {
            ResonanceTransporter t1 = new ResonanceTransporter(new TcpAdapter(TcpAdapter.GetLocalIPAddress(), 15999));
            ResonanceTransporter t2 = new ResonanceTransporter();

            ResonanceTcpServer server = new ResonanceTcpServer(15999);
            server.Start();
            server.ConnectionRequest += (x, e) => 
            {
                t2.Adapter = e.Accept();
                t2.Connect();
            };

            t1.Connect();

            while (t2.State != ResonanceComponentState.Connected)
            {
                Thread.Sleep(10);
            }

            TestUtils.Read_Write_Test(this, t1, t2, false, 1000, 5);

            server.Dispose();
        }

        [TestMethod]
        public void Udp_Adapter_Writing_Reading()
        {
            IPAddress localIpAddress = IPAddress.Parse(TcpAdapter.GetLocalIPAddress());

            TestUtils.Read_Write_Test(
                this, 
                new UdpAdapter(new IPEndPoint(localIpAddress, 15991), new IPEndPoint(localIpAddress, 15992)), 
                new UdpAdapter(new IPEndPoint(localIpAddress, 15992), new IPEndPoint(localIpAddress, 15991)), 
                false, 
                1000, 
                5);
        }

        [TestMethod]
        public void Usb_Adapter_Writing_Reading()
        {
            if (IsRunningOnAzurePipelines)
            {
                return;
            }

            String virtualSerialDeviceName = "HHD Software Virtual Serial Port";
            String errorMessage = "Could not locate any virtual serial port bridge. Please download from https://freevirtualserialports.com and create a local bridge.";

            var devices = UsbDevice.GetAvailableDevices();

            var virtualPort1 = devices.FirstOrDefault(x => x.Description.Contains(virtualSerialDeviceName));
            Assert.IsNotNull(virtualPort1, errorMessage);

            var virtualPort2 = devices.FirstOrDefault(x => x.Description.Contains(virtualSerialDeviceName) && x != virtualPort1);
            Assert.IsNotNull(virtualPort2, errorMessage);

            TestUtils.Read_Write_Test(
                this,
                new UsbAdapter(virtualPort1, BaudRates.BR_19200),
                new UsbAdapter(virtualPort2, BaudRates.BR_19200),
                false,
                1000,
                10);
        }

        [TestMethod]
        public void NamedPipes_Adapter_Writing_Reading()
        {
            if (IsRunningOnAzurePipelines) return;

            ResonanceTransporter t1 = new ResonanceTransporter(new NamedPipesAdapter("Resonance"));
            ResonanceTransporter t2 = new ResonanceTransporter();

            ResonanceNamedPipesServer server = new ResonanceNamedPipesServer("Resonance");
            server.Start();
            server.ConnectionRequest += (x, e) =>
            {
                t2.Adapter = e.Accept();
                t2.Connect();
            };

            t1.Connect();

            while (t2.State != ResonanceComponentState.Connected)
            {
                Thread.Sleep(10);
            }

            TestUtils.Read_Write_Test(this, t1, t2, false, 1000, 5);

            server.Dispose();
        }

        [TestMethod]
        public void Shared_Memory_Adapter_Writing_Reading()
        {
            TestUtils.Read_Write_Test(
                this,
                new SharedMemoryAdapter("TST"),
                new SharedMemoryAdapter("TST"),
                false,
                1000,
                2);
        }

        public class LargePayloadRequest
        {
            public String Payload { get; set; }
        }

        public class LargePayloadResponse
        {
            public String Payload { get; set; }
        }

        [TestMethod]
        public void Shared_Memory_Adapter_Sends_Message_Larger_Than_The_Old_Fixed_Buffer()
        {
            // The buffer used to be hardcoded at 1000 bytes, so anything over roughly 996
            // bytes failed with "Not enough space available in the buffer". The size is now
            // configurable and defaults to 1 MB.
            String address = "TST-LARGE";

            IResonanceTransporter t1 = ResonanceTransporter.Builder.Create()
                .WithAdapter(new SharedMemoryAdapter(address))
                .WithJsonTranscoding()
                .NoKeepAlive()
                .Build();

            IResonanceTransporter t2 = ResonanceTransporter.Builder.Create()
                .WithAdapter(new SharedMemoryAdapter(address))
                .WithJsonTranscoding()
                .NoKeepAlive()
                .Build();

            try
            {
                t1.Connect();
                t2.Connect();

                t2.RequestReceived += (s, e) =>
                {
                    LargePayloadRequest received = e.Message.Object as LargePayloadRequest;
                    t2.SendResponse(new LargePayloadResponse() { Payload = received.Payload }, e.Message.Token);
                };

                //Comfortably beyond the old ~996 byte ceiling.
                String payload = new String('x', 64 * 1024);

                var response = t1.SendRequest<LargePayloadRequest, LargePayloadResponse>(
                    new LargePayloadRequest() { Payload = payload });

                Assert.AreEqual(payload, response.Payload);
            }
            finally
            {
                t1.Dispose(true);
                t2.Dispose(true);
            }
        }

        [TestMethod]
        public void Shared_Memory_Adapter_Reports_Message_Too_Large()
        {
            SharedMemoryAdapter adapter = new SharedMemoryAdapter("TST-SMALL", 2048);

            Assert.AreEqual(2048, adapter.BufferSize);
            Assert.AreEqual(2044, adapter.MaxMessageSize);

            //A buffer too small to carry the connection handshake is rejected up front.
            Assert.Throws<ArgumentOutOfRangeException>(() => new SharedMemoryAdapter("TST-TINY", 8));
        }
    }
}
