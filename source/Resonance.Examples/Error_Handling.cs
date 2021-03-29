using Resonance.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.Examples
{
    public class Error_Handling
    {
        public async void Demo()
        {
            IResonanceTransporter transporter1 = ResonanceTransporter.Builder
               .Create()
               .WithInMemoryAdapter()
               .WithAddress("TEST")
               .WithJsonTranscoding()
               .Build();

            IResonanceTransporter transporter2 = ResonanceTransporter.Builder
                .Create()
                .WithInMemoryAdapter()
                .WithAddress("TEST")
                .WithJsonTranscoding()
                .Build();

            await transporter1.Connect();
            await transporter2.Connect();

            transporter1.RegisterRequestHandler<CalculateRequest>(async (t, request) => 
            {
                try
                {
                    double sum = request.Message.A / request.Message.B;
                }
                catch (DivideByZeroException ex)
                {
                    await t.SendErrorResponse(ex, request.Token);
                }
            });


            try
            {
                var response = await transporter2.SendRequest<CalculateRequest, CalculateResponse>(new CalculateRequest());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
