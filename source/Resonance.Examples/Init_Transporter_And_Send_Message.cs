﻿using Resonance.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.Examples
{
    class Init_Transporter_And_Send_Message
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