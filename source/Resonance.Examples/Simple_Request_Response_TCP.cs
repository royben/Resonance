using Resonance.Messages;
using Resonance.Servers.Tcp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.Examples
{
    class Simple_Request_Response_TCP
    {
        public async void Demo()
        {
            ResonanceTcpServer server = new ResonanceTcpServer(8888);
            server.ConnectionRequest += Server_ConnectionRequest;
            await server.Start();

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

            var response = await transporter1.SendRequest<CalculateRequest, CalculateResponse>(new CalculateRequest()
            {
                A = 10,
                B = 5
            });

            Console.WriteLine(response.Sum);
        }

        private async void Server_ConnectionRequest(object sender, ResonanceListeningServerConnectionRequestEventArgs<Adapters.Tcp.TcpAdapter> e)
        {
            IResonanceTransporter transporter2 = ResonanceTransporter.Builder
                .Create()
                .WithAdapter(e.Accept())
                .WithJsonTranscoding()
                .WithKeepAlive()
                .NoEncryption()
                .WithCompression()
                .Build();

            transporter2.RequestReceived += Transporter2_RequestReceived;

            await transporter2.Connect();
        }

        private void Transporter2_RequestReceived(object sender, ResonanceRequestReceivedEventArgs e)
        {
            CalculateRequest receivedRequest = e.Request.Message as CalculateRequest;
            (sender as IResonanceTransporter).SendResponse(new CalculateResponse() 
            {
                Sum = receivedRequest.A + receivedRequest.B 
            }, e.Request.Token);
        }
    }
}
