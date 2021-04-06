using Resonance.Adapters.Tcp;
using Resonance.Messages;
using Resonance.Transcoding.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.Examples
{
    class Init_Transporter_And_Send_Message
    {
        public async void Demo_Standard()
        {
            IResonanceTransporter transporter = new ResonanceTransporter();

            transporter.Adapter = new TcpAdapter("127.0.0.1", 8888);
            transporter.Encoder = new JsonEncoder();
            transporter.Decoder = new JsonDecoder();
            transporter.KeepAliveConfiguration.Enabled = true;
            transporter.Encoder.CompressionConfiguration.Enabled = true;
            transporter.CryptographyConfiguration.Enabled = true;

            await transporter.Connect();

            var response = await transporter.SendRequest<CalculateRequest, CalculateResponse>(new CalculateRequest()
            {
                A = 10,
                B = 5
            });

            Console.WriteLine(response.Sum);
        }

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

            await transporter.Connect();

            var response = await transporter.SendRequest<CalculateRequest, CalculateResponse>(new CalculateRequest()
            {
                A = 10,
                B = 5
            });

            Console.WriteLine(response.Sum);
        }
    }
}
