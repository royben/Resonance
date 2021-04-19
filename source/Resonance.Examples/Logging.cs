using Microsoft.Extensions.Logging;
using Resonance.Messages;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.Examples
{
    public class Logging
    {
        public void InitLogging()
        {
            var loggerFactory = new LoggerFactory();
            var logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.Seq("http://localhost:5341")
                .CreateLogger();

            loggerFactory.AddSerilog(logger);

            ResonanceGlobalSettings.Default.LoggerFactory = loggerFactory;
        }

        public async void Demo()
        {
            IResonanceTransporter transporter1 = ResonanceTransporter.Builder
               .Create().WithTcpAdapter()
               .WithAddress("127.0.0.1")
               .WithPort(8888)
               .WithJsonTranscoding()
               .WithKeepAlive()
               .NoEncryption()
               .WithCompression()
               .Build();

            await transporter1.Connect();

            CalculateRequest request = new CalculateRequest() { A = 10, B = 5 };

            //Log request and response names
            var response = await transporter1.SendRequest<CalculateRequest, CalculateResponse>(request,
                new ResonanceRequestConfig() { LoggingMode = ResonanceMessageLoggingMode.Title });

            //Log request and response names and content
            response = await transporter1.SendRequest<CalculateRequest, CalculateResponse>(request,
                new ResonanceRequestConfig() { LoggingMode = ResonanceMessageLoggingMode.Content });
        }
    }
}
