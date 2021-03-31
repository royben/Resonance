//using Resonance.SignalR;
using Resonance.SignalR;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.Tests.CLI
{
    class Program
    {
        static void Main(string[] args)
        {


            //ResonancePerformanceTester tester = new ResonancePerformanceTester();
            //tester.TestTcpAdapterPerformance();

            //if (Debugger.IsAttached)
            //{
            //    Console.WriteLine("Press enter to continue...");
            //    Console.ReadLine();
            //}

            ResonanceSignalRClient client = new ResonanceSignalRClient("http://localhost:5515/hubs/resonance");
            client.Start();

            String txt = String.Empty;

            while (txt != "exit")
            {
                txt = Console.ReadLine();
                String output = client.GetString(txt);
                Console.WriteLine(output);
                Console.WriteLine();
            }

            Console.WriteLine("Press any key to exit.");
            Console.ReadLine();
        }
    }
}
