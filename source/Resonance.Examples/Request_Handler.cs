using Resonance.Messages;
using Resonance.Tcp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.Examples
{
    class Request_Handler
    {
        public async void Demo()
        {
            IResonanceTransporter transporter = ResonanceTransporter.Builder
                .Create().WithTcpAdapter()
                .WithAddress("127.0.0.1")
                .WithPort(8888)
                .WithJsonTranscoding()
                .WithKeepAlive()
                .NoEncryption()
                .WithCompression()
                .Build();

            transporter.RegisterRequestHandler<CalculateRequest>(HandleCalculateRequest);

            await transporter.Connect();
        }

        private async void HandleCalculateRequest(IResonanceTransporter transporter, ResonanceRequest<CalculateRequest> request)
        {
            await transporter.SendResponse(new CalculateResponse() { Sum = request.Message.A + request.Message.B }, request.Token);
        }
    }
}
