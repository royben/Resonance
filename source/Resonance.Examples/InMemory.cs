using Resonance.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.Examples
{
    class InMemory
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
        }
    }
}
//Easily 
