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

namespace Resonance.Tests
{
    [TestClass]
    [TestCategory("Adapters")]
    public class Adapters_TST : ResonanceTest
    {
        [TestMethod]
        public async Task InMemory_Adapter_Writing_Reading()
        {
            Init();
            await TestUtils.Read_Write_Test(
                this, 
                new InMemoryAdapter("TST"), 
                new InMemoryAdapter("TST"), 
                false, 
                false, 
                1000, 
                2);
        }

        [TestMethod]
        public async Task Tcp_Adapter_Writing_Reading()
        {
            Init();

            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new TcpAdapter(TcpAdapter.GetLocalIPAddress(), 9999));
            ResonanceJsonTransporter t2 = new ResonanceJsonTransporter();

            ResonanceTcpServer server = new ResonanceTcpServer(9999);
            await server.Start();
            server.ConnectionRequest += async (x, e) => 
            {
                t2.Adapter = e.Accept();
                await t2.Connect();
            };

            await t1.Connect();

            while (t2.State != ResonanceComponentState.Connected)
            {
                Thread.Sleep(10);
            }

            await TestUtils.Read_Write_Test(this, t1, t2, false, false, 1000, 5);

            await server.DisposeAsync();
        }

        [TestMethod]
        public async Task Udp_Adapter_Writing_Reading()
        {
            Init();

            IPAddress localIpAddress = IPAddress.Parse(TcpAdapter.GetLocalIPAddress());

            await TestUtils.Read_Write_Test(
                this, 
                new UdpAdapter(new IPEndPoint(localIpAddress, 9991), new IPEndPoint(localIpAddress, 9992)), 
                new UdpAdapter(new IPEndPoint(localIpAddress, 9992), new IPEndPoint(localIpAddress, 9991)), 
                false, 
                false, 
                1000, 
                5);
        }

        [TestMethod]
        public async Task Usb_Adapter_Writing_Reading()
        {
            Init();

            if (IsRunningOnAzurePipelines)
            {
                Log.Info("Running on azure. Skipping USB Adapter test.");
                return;
            }

            String virtualSerialDeviceName = "HHD Software Virtual Serial Port";
            String errorMessage = "Could not locate any virtual serial port bridge. Please download from https://freevirtualserialports.com and create a local bridge.";

            var devices = await UsbDevice.GetAvailableDevices();

            var virtualPort1 = devices.FirstOrDefault(x => x.Description.Contains(virtualSerialDeviceName));
            Assert.IsNotNull(virtualPort1, errorMessage);

            var virtualPort2 = devices.FirstOrDefault(x => x.Description.Contains(virtualSerialDeviceName) && x != virtualPort1);
            Assert.IsNotNull(virtualPort2, errorMessage);

            await TestUtils.Read_Write_Test(
                this,
                new UsbAdapter(virtualPort1, BaudRates.BR_19200),
                new UsbAdapter(virtualPort2, BaudRates.BR_19200),
                false,
                false,
                1000,
                10);
        }

        [TestMethod]
        public async Task NamedPipes_Adapter_Writing_Reading()
        {
            Init();

            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new NamedPipesAdapter("Resonance"));
            ResonanceJsonTransporter t2 = new ResonanceJsonTransporter();

            t1.DisableHandShake = true;
            t2.DisableHandShake = true;

            ResonanceNamedPipesServer server = new ResonanceNamedPipesServer("Resonance");
            await server.Start();
            server.ConnectionRequest += async (x, e) =>
            {
                t2.Adapter = e.Accept();
                await t2.Connect();
            };

            await t1.Connect();

            while (t2.State != ResonanceComponentState.Connected)
            {
                Thread.Sleep(10);
            }

            await TestUtils.Read_Write_Test(this, t1, t2, false, false, 1000, 5);

            await server .DisposeAsync();
        }
    }
}
