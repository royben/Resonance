using Resonance.RPC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.Examples.UDP.BroadcastClient
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting Resonance UDP Broadcast Demo...");

            Console.WriteLine("Connecting transporter with UDP adapter on all addresses...");

            //Initialize a transporter with UDP adapter. Listening and sending to all interfaces.
            IResonanceTransporter transporter = ResonanceTransporter.Builder
                .Create()
                .WithUdpAdapter()
                .Broadcast(1234)
                .PreventLoopback()
                .WithJsonTranscoding()
                .Build();

            //Connect the transporter.
            transporter.Connect();

            //Register the service to handle incoming messages from other clients.
            transporter.RegisterService<IRPCBroadcastService, RPCBroadcastService>(RpcServiceCreationType.Singleton);

            //Create a service client proxy to send messages to other clients.
            IRPCBroadcastService client = transporter.CreateClientProxy<IRPCBroadcastService>();

            Console.WriteLine("Now connected. Make sure at least one more client is also connected before sending messages.");
            Console.WriteLine();

            String input = String.Empty;

            while (input.ToLower() != "exit")
            {
                Console.Write("Enter a message to broadcast: ");
                input = Console.ReadLine();

                try
                {
                    client.BroadcastMessage(input);
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(ex.Message);
                    Console.ForegroundColor = ConsoleColor.Gray;
                }

                Console.WriteLine();
            }
        }
    }

    public interface IRPCBroadcastService
    {
        [ResonanceRpc(RequireACK = false)]
        void BroadcastMessage(String message);
    }

    public class RPCBroadcastService : IRPCBroadcastService
    {
        public void BroadcastMessage(string message)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Received: " + message);
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine();
            Console.Write("Enter a message to broadcast: ");
        }
    }
}
