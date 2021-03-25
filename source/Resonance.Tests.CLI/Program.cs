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
            ResonancePerformanceTester tester = new ResonancePerformanceTester();
            tester.TestTcpAdapterPerformance();

            if (Debugger.IsAttached)
            {
                Console.WriteLine("Press enter to continue...");
                Console.ReadLine();
            }
        }
    }
}
