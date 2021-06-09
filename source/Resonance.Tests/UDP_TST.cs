using Microsoft.VisualStudio.TestTools.UnitTesting;
using Resonance.Adapters.Udp;
using Resonance.Servers.Udp;
using Resonance.Tests.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Resonance.Tests
{
    [TestClass]
    [TestCategory("UDP")]
    public class UDP_TST : ResonanceTest
    {

        [TestMethod]
        public void UdpAdapter_Throws_Exception_After_Server_Shutdown()
        {
            ResonanceUdpServer udp = new ResonanceUdpServer(9999);
            udp.Start();
            udp.Dispose();

            UdpAdapter adapter = new UdpAdapter("127.0.0.1", 9999);

            Assert.ThrowsException<SocketException>(() =>
            {
                adapter.Connect();
            });

            adapter.Dispose();
        }

        [TestMethod]
        public void UdpAdapter_Throws_Exception_When_No_Server()
        {
            UdpAdapter adapter = new UdpAdapter("127.0.0.1", 9999);

            Assert.ThrowsException<SocketException>(() =>
            {
                adapter.Connect();
            });

            adapter.Dispose();
        }

        [TestMethod]
        public void UdpAdapter_Throws_Exception_On_Connection_Timeout()
        {
            ResonanceUdpServer udp = new ResonanceUdpServer(9999);
            udp.ConnectionRequest += (x, e) =>
            {
                //e.Decline();
            };
            udp.Start();

            UdpAdapter adapter = new UdpAdapter("127.0.0.1", 9999);

            Assert.ThrowsException<TimeoutException>(() =>
            {
                adapter.Connect();
            });

            adapter.Dispose();
            udp.Dispose();
        }

        [TestMethod]
        public void UdpAdapter_Throws_Exception_On_Connection_Declined()
        {
            ResonanceUdpServer udp = new ResonanceUdpServer(9999);
            udp.ConnectionRequest += (x, e) =>
            {
                e.Decline();
            };
            udp.Start();

            UdpAdapter adapter = new UdpAdapter("127.0.0.1", 9999);

            Assert.ThrowsException<Exception>(() =>
            {
                adapter.Connect();
            });

            adapter.Dispose();
            udp.Dispose();
        }

        [TestMethod]
        public void UdpAdapter_Connection_Success()
        {
            ResonanceUdpServer udp = new ResonanceUdpServer(9999);
            udp.ConnectionRequest += (x, e) =>
            {
                e.Accept();
            };
            udp.Start();

            UdpAdapter adapter = new UdpAdapter("127.0.0.1", 9999);

            adapter.Connect();

            Assert.IsTrue(adapter.State == ResonanceComponentState.Connected);

            adapter.Dispose();
            udp.Dispose();
        }

        private class UdpAdapterEntry
        {
            public UdpAdapter ServerAdapter { get; set; }
            public UdpAdapter ClientAdapter { get; set; }
            public int MessageCount { get; set; }
        }

        [TestMethod]
        public void UdpAdapter_Communication_Is_One_On_One()
        {
            List<UdpAdapterEntry> entries = new List<UdpAdapterEntry>();

            ResonanceUdpServer udp = new ResonanceUdpServer(9999);
            udp.ConnectionRequest += (x, e) =>
            {
                var serverAdapter = e.Accept();
                var entry = entries.Last();
                entry.ServerAdapter = serverAdapter;
                serverAdapter.DataAvailable += (_, __) =>
                {
                    entry.MessageCount++;
                    Task.Factory.StartNew(() =>
                    {
                        serverAdapter.Write(new byte[] { 4, 3, 2, 1 });
                    });
                };

                serverAdapter.Connect();
            };
            udp.Start();

            for (int i = 0; i < 10; i++)
            {
                UdpAdapter adapter = new UdpAdapter("127.0.0.1", 9999);
                var entry = new UdpAdapterEntry() { ClientAdapter = adapter };
                entries.Add(entry);
                adapter.DataAvailable += (_, __) =>
                {
                    entry.MessageCount++;
                };
                adapter.Connect();
                Thread.Sleep(100);
                adapter.Write(new byte[] { 1, 2, 3, 4 });
            }

            Thread.Sleep(5000);

            foreach (var entry in entries)
            {
                Assert.IsTrue(entry.MessageCount == 2);
                entry.ClientAdapter.Dispose();
                entry.ServerAdapter.Dispose();
            }

            udp.Dispose();
        }
    }
}
