using Resonance.Adapters.Tcp;
using Resonance.Messages;
using Resonance.Servers.Tcp;
using Resonance.Transporters;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Resonance.Tests.CLI
{
    public class ResonancePerformanceTester
    {
        public void TestTcpAdapterPerformance()
        {
            Console.WriteLine("Starting TCP Adapter Performance Test");

            ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new TcpAdapter("127.0.0.1", 9999));
            ResonanceJsonTransporter t2 = new ResonanceJsonTransporter();

            ResonanceTcpServer server = new ResonanceTcpServer(9999);
            server.Start();
            server.ClientConnected += (x, e) =>
            {
                t2.Adapter = new TcpAdapter(e.TcpClient);
                t2.Connect().Wait();
            };

            t1.Connect().Wait();

            while (t1.State != ResonanceComponentState.Connected)
            {
                Thread.Sleep(10);
            }

            t2.RequestReceived += (s, e) =>
            {
                CalculateRequest receivedRequest = e.Request.Message as CalculateRequest;
                t2.SendResponse(new CalculateResponse() { Sum = receivedRequest.A + receivedRequest.B }, e.Request.Token);
            };

            Stopwatch watch = new Stopwatch();

            for (int i = 0; i < 100000; i++)
            {
                watch.Restart();
                var request = new CalculateRequest() { A = 10, B = i };
                var response = t1.SendRequest<CalculateRequest, CalculateResponse>(request, new ResonanceRequestConfig()
                {
                    //CancellationToken = new CancellationToken()
                }).GetAwaiter().GetResult();

                long elapsed = watch.ElapsedMilliseconds;

                if (elapsed > 20)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Gray;
                }

                Console.WriteLine($"Request/Response Time: {elapsed} ms");
            }

            t1.Dispose(true);
            t2.Dispose(true);
            server.Dispose();

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("TCP Adapter Performance Test Completed");
        }
    }
}
