using Resonance.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.Examples
{
    class Services
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

            transporter.RegisterService(new MyResonanceService());

            await transporter.Connect();
        }

        private class MyResonanceService : IResonanceService
        {
            public ResonanceActionResult<CalculateResponse> Calculate(CalculateRequest request)
            {
                return new CalculateResponse() { Sum = request.A + request.B };
            }

            public void OnTransporterStateChanged(ResonanceComponentState state)
            {
                if (state == ResonanceComponentState.Failed)
                {
                    //Connection lost
                }
            }
        }
    }
}
