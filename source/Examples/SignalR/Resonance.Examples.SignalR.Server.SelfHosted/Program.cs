using Microsoft.Owin.Hosting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.Examples.SignalR.Server
{
    class Program
    {
        static void Main(string[] args)
        {
            string address = "http://localhost:8080/";

            // Start OWIN host 
            using (WebApp.Start<Startup>(address))
            {
                Console.WriteLine("SignalR Demo Server Started...");
                Process.Start(address);
                Console.ReadLine();
            }
        }
    }
}
