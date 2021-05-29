using Microsoft.Owin.Hosting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.Examples.WebRTC.Server
{
    class Program
    {
        static void Main(string[] args)
        {
            string address = "http://localhost:8081/";

            // Start OWIN host 
            using (WebApp.Start<Startup>(address))
            {
                Console.WriteLine("WebRTC Signaling Demo Server Started...");
                Process.Start(address);
                Console.ReadLine();
            }
        }
    }
}
