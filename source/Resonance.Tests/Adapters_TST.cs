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
using Resonance.Tcp;
using Resonance.Tests.Common;
using Resonance.Messages;
using Resonance.Transporters;

namespace Resonance.Tests
{
    [TestClass]
    [TestCategory("Adapters")]
    public class Adapters_TST : ResonanceTest
    {
        [TestMethod]
        public void Tcp_Adapter_Writing_Reading()
        {
            Init();

            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new TcpAdapter(TcpAdapter.GetLocalIPAddress(), 9999));
            ResonanceJsonTransporter t2 = new ResonanceJsonTransporter();

            ResonanceTcpServer server = new ResonanceTcpServer(9999);
            server.Start();
            server.ClientConnected += (x, e) =>
            {
                t2.Adapter = new TcpAdapter(e.TcpClient);
                t2.Connect().Wait();
            };

            t1.Connect().Wait();

            while (t2.State != ResonanceComponentState.Connected)
            {
                Thread.Sleep(10);
            }

            t2.RequestReceived += (s, e) =>
            {
                CalculateRequest receivedRequest = e.Request.Message as CalculateRequest;
                t2.SendResponse(new CalculateResponse() { Sum = receivedRequest.A + receivedRequest.B }, e.Request.Token);
            };

            Stopwatch watch = new Stopwatch();

            List<double> measurements = new List<double>();

            for (int i = 0; i < 1000; i++)
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
            server.Dispose();

            var outliers = TestHelper.GetOutliers(measurements);

            double percentageOfOutliers = outliers.Count / (double)measurements.Count * 100d;

            if (!IsRunningOnAzurePipelines)
            {
                Assert.IsTrue(percentageOfOutliers < 2, $"Request/Response duration measurements contains {percentageOfOutliers}% outliers and is considered a performance issue.");
            }
        }

        [TestMethod]
        public void Udp_Adapter_Writing_Reading()
        {
            Init();

            IPAddress localIpAddress = IPAddress.Parse(TcpAdapter.GetLocalIPAddress());

            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new UdpAdapter(new IPEndPoint(localIpAddress, 9991), new IPEndPoint(localIpAddress, 9992)));
            ResonanceJsonTransporter t2 = new ResonanceJsonTransporter(new UdpAdapter(new IPEndPoint(localIpAddress, 9992), new IPEndPoint(localIpAddress, 9991)));

            t1.Connect().Wait();
            t2.Connect().Wait();

            t2.RequestReceived += (s, e) =>
            {
                CalculateRequest receivedRequest = e.Request.Message as CalculateRequest;
                t2.SendResponse(new CalculateResponse() { Sum = receivedRequest.A + receivedRequest.B }, e.Request.Token);
            };

            Stopwatch watch = new Stopwatch();

            List<double> measurements = new List<double>();

            for (int i = 0; i < 1000; i++)
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

            var outliers = TestHelper.GetOutliers(measurements);

            double percentageOfOutliers = outliers.Count / (double)measurements.Count * 100d;

            if (!IsRunningOnAzurePipelines)
            {
                Assert.IsTrue(percentageOfOutliers < 2, $"Request/Response duration measurements contains {percentageOfOutliers}% outliers and is considered a performance issue.");
            }
        }

        [TestMethod]
        public void Usb_Adapter_Writing_Reading()
        {
            Init();

            if (IsRunningOnAzurePipelines)
            {
                LogManager.Log("Running on azure. Skipping USB Adapter test.");
                return;
            }

            String virtualSerialDeviceName = "HHD Software Virtual Serial Port";
            String errorMessage = "Could not locate any virtual serial port bridge. Please download from https://freevirtualserialports.com and create a local bridge.";

            var devices = UsbDevice.GetAvailableDevices().GetAwaiter().GetResult();

            var virtualPort1 = devices.FirstOrDefault(x => x.Description.Contains(virtualSerialDeviceName));
            Assert.IsNotNull(virtualPort1, errorMessage);

            var virtualPort2 = devices.FirstOrDefault(x => x.Description.Contains(virtualSerialDeviceName) && x != virtualPort1);
            Assert.IsNotNull(virtualPort2, errorMessage);

            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new UsbAdapter(virtualPort1, BaudRates.BR_19200));
            ResonanceJsonTransporter t2 = new ResonanceJsonTransporter(new UsbAdapter(virtualPort2, BaudRates.BR_19200));

            t1.Connect().Wait();
            t2.Connect().Wait();

            t2.RequestReceived += (s, e) =>
            {
                CalculateRequest receivedRequest = e.Request.Message as CalculateRequest;
                t2.SendResponse(new CalculateResponse() { Sum = receivedRequest.A + receivedRequest.B }, e.Request.Token);
            };

            Stopwatch watch = new Stopwatch();

            List<double> measurements = new List<double>();

            for (int i = 0; i < 1000; i++)
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

            var outliers = TestHelper.GetOutliers(measurements);

            double percentageOfOutliers = outliers.Count / (double)measurements.Count * 100d;

            Assert.IsTrue(percentageOfOutliers < 10, $"Request/Response duration measurements contains {percentageOfOutliers}% outliers and is considered a performance issue.");
        }
    }
}
