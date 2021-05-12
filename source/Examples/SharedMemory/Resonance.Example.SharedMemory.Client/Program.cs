using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.Example.SharedMemory.Client
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "Resonance Shared Memory Demo";

            Console.WriteLine("Resonance Shared Memory Demo.");
            Console.WriteLine();

            Console.Write("Adapter Address: ");
            String address = Console.ReadLine();

            Console.WriteLine("Connecting...");

            IResonanceTransporter transporter = ResonanceTransporter.Builder
                .Create()
                .WithSharedMemoryAdapter()
                .WithAddress(address)
                .WithJsonTranscoding()
                .Build();

            transporter.StateChanged += (_, e) => 
            {
                if (e.NewState == ResonanceComponentState.Failed)
                {
                    Console.WriteLine();
                    Console.WriteLine($"Transporter Disconnected.\n{transporter.FailedStateException.Message}");
                }
            };

            transporter.RegisterMessageHandler<SharedMemoryMessage>((t, msg) => 
            {
                Console.WriteLine();
                Console.WriteLine($"Message Received: {msg.Object.Text}");
                Console.Write("Send Message: ");
            });

            transporter.Connect();

            Console.WriteLine("Connected.");

            while (true)
            {
                Console.Write("Send Message: ");
                String text = Console.ReadLine();

                if (text.ToLower() == "exit")
                {
                    Console.WriteLine("Disconnecting...");
                    transporter.Disconnect();
                    Console.ReadLine();
                    return;
                }

                transporter.Send(new SharedMemoryMessage() { Text = text });
            }
        }
    }
}
