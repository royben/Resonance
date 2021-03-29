using Resonance.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Resonance.Examples
{
    class Continuous_Request
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

            transporter2.RegisterRequestHandler<ProgressRequest>(async (t, request) =>
            {
                for (int i = 0; i < request.Message.Count; i++)
                {
                    await t.SendResponse(new ProgressResponse() { Value = i }, request.Token);
                    Thread.Sleep(request.Message.Interval);
                }
            });

            transporter1.SendContinuousRequest<ProgressRequest, ProgressResponse>(new ProgressRequest()
            {
                Interval = TimeSpan.FromSeconds(1),
                Count = 10
            }).Subscribe((response) =>
            {
                Console.WriteLine(response.Value);
            }, (ex) =>
            {
                Console.WriteLine($"Error: {ex.Message}");
            }, () =>
            {
                Console.WriteLine($"Continuous Request Completed!");
            });
        }
    }
}
