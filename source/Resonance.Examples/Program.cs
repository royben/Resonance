using Resonance.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.Examples
{
    class Program
    {
        static void Main(string[] args)
        {

        }
    }

    public class InitTransporterAndSendAMessage
    {
        public async void Demo()
        {
            IResonanceTransporter transporter = ResonanceTransporter.Builder
                .Create()
                .WithTcpAdapter()
                .WithAddress("127.0.0.1")
                .WithPort(8888)
                .WithJsonTranscoding()
                .WithKeepAlive()
                .NoEncryption()
                .WithCompression()
                .Build();

            var response = await transporter.SendRequest<CalculateRequest, CalculateResponse>(new CalculateRequest()
            {
                A = 10,
                B = 5
            });

            Console.WriteLine(response.Sum);
        }
    }
}
