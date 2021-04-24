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
using Resonance.Transporters;
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
                false, 
                1000, 
                2);
        }

        [TestMethod]
        public void Tcp_Adapter_Writing_Reading()
        {
            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new TcpAdapter(TcpAdapter.GetLocalIPAddress(), 9999));
            ResonanceJsonTransporter t2 = new ResonanceJsonTransporter();

            ResonanceTcpServer server = new ResonanceTcpServer(9999);
            server.Start().GetAwaiter().GetResult();
            server.ConnectionRequest += (x, e) => 
            {
                t2.Adapter = e.Accept();
                t2.Connect().GetAwaiter().GetResult();
            };

            t1.Connect().GetAwaiter().GetResult();

            while (t2.State != ResonanceComponentState.Connected)
            {
                Thread.Sleep(10);
            }

            TestUtils.Read_Write_Test(this, t1, t2, false, false, 1000, 5);

            server.Dispose();
        }

        [TestMethod]
        public void Udp_Adapter_Writing_Reading()
        {
            IPAddress localIpAddress = IPAddress.Parse(TcpAdapter.GetLocalIPAddress());

            TestUtils.Read_Write_Test(
                this, 
                new UdpAdapter(new IPEndPoint(localIpAddress, 9991), new IPEndPoint(localIpAddress, 9992)), 
                new UdpAdapter(new IPEndPoint(localIpAddress, 9992), new IPEndPoint(localIpAddress, 9991)), 
                false, 
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

            var devices = UsbDevice.GetAvailableDevices().GetAwaiter().GetResult();

            var virtualPort1 = devices.FirstOrDefault(x => x.Description.Contains(virtualSerialDeviceName));
            Assert.IsNotNull(virtualPort1, errorMessage);

            var virtualPort2 = devices.FirstOrDefault(x => x.Description.Contains(virtualSerialDeviceName) && x != virtualPort1);
            Assert.IsNotNull(virtualPort2, errorMessage);

            TestUtils.Read_Write_Test(
                this,
                new UsbAdapter(virtualPort1, BaudRates.BR_19200),
                new UsbAdapter(virtualPort2, BaudRates.BR_19200),
                false,
                false,
                1000,
                10);
        }

        [TestMethod]
        public void NamedPipes_Adapter_Writing_Reading()
        {
            if (IsRunningOnAzurePipelines) return;

            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new NamedPipesAdapter("Resonance"));
            ResonanceJsonTransporter t2 = new ResonanceJsonTransporter();

            t1.DisableHandShake = true;
            t2.DisableHandShake = true;

            ResonanceNamedPipesServer server = new ResonanceNamedPipesServer("Resonance");
            server.Start().GetAwaiter().GetResult();
            server.ConnectionRequest += (x, e) =>
            {
                t2.Adapter = e.Accept();
                t2.Connect().GetAwaiter().GetResult();
            };

            t1.Connect().GetAwaiter().GetResult();

            while (t2.State != ResonanceComponentState.Connected)
            {
                Thread.Sleep(10);
            }

            TestUtils.Read_Write_Test(this, t1, t2, false, false, 1000, 5);

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
                false,
                1000,
                2);
        }
    }
}
