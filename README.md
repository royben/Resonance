<p align="center">
  <img src="https://github.com/royben/Resonance/blob/dev/visuals/Logo.png" />    
</p>

# Resonance <img width="20" height="20" src="https://github.com/royben/Resonance/blob/dev/visuals/icon.png" /> [![Build Status](https://sirilix.visualstudio.com/Resonance/_apis/build/status/royben.Resonance?branchName=main)](https://sirilix.visualstudio.com/Resonance/_build/latest?definitionId=1&branchName=main) ![Issues](https://img.shields.io/github/issues/royben/Resonance.svg)

Resonance is a high-performance real-time C# communication library with built-in support for several different transcoding and delivery methods.
This library provides an intuitive API for asynchronous communication between machines and devices by exposing a set of easy to use, pluggable components.

<br/>
<br/>
The resonance library might be described by the folowing layers:

### Transporting
A transporter responsibility is to provide the API for sending and receiving messages, managing those messages, and propagating the necessary information to other components.

### Transcoding
Encoders and Decoders are components that can be plugged to a transporter, they determine how outgoing/incoming messages should be encoded and whether the data should be encrypted and/or compressed.
The Following built-in transcoding methods are currently supported by the library:
*	Json - <span style="color:gray">(using Json.NET)</span>.
*	Bson - <span style="color:gray">(using (using Json.NET))</span>.
*	Protobuf - <span style="color:gray">(using Google.Protobuf separate NuGet package)</span>.
*	Xml - <span style="color:gray">(using .NET built-in Xml Serializer)</span>.

### Adapters
Adapters can also be plugged to a transporter to determine how outgoing/incoming encoded data is going to be transmitted and where.
The following built-in adapters are currently supported by the library:
*	TCP
*	UDP
*	USB
*	HTTP
*	In-Memory
*	SignalR
*	WebRTC
*	Named Pipes
*	Shared Memory

The following diagram described a simple request-response scenario.

![alt tag](https://github.com/royben/Resonance/blob/dev/visuals/Resonance_Flow.png)

# Usage Examples

<br/>

#### Create a Transporter and send a message.
```c#
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
```
<br/>
<br/>

#### Simple TCP request response.
```c#
        public async void Demo()
        {
            ResonanceTcpServer server = new ResonanceTcpServer(8888);
            server.Start();
            server.ClientConnected += Server_ClientConnected;

            IResonanceTransporter transporter1 = ResonanceTransporter.Builder
                .Create()
                .WithTcpAdapter()
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

        private async void Server_ClientConnected(object sender, ResonanceTcpServerClientConnectedEventArgs e)
        {
            IResonanceTransporter transporter2 = ResonanceTransporter.Builder
                .Create()
                .WithTcpAdapter()
                .FromTcpClient(e.TcpClient)
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
```
<br/>
<br/>

#### Using a request handler.
```c#
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
```
<br/>
<br/>

#### Registering a Resonance Service.
```c#
        public void Demo()
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
```

<br/>
<br/>
<br/>

# Benchmarks
>1000 Roundtrips (request -> response), Intel Core i7-6700HQ CPU 2.60GHz (Skylake)

**Transcoding**

|                                       Method |     Mean |   Error |  StdDev |
|--------------------------------------------- |---------:|--------:|--------:|
| Json | 205.6 ms | 4.09 ms | 9.96 ms |
| Protobuf | 207.3 ms | 4.07 ms | 3.81 ms |

<br/>

**Encryption / Compression**

|                                                              Method |     Mean |   Error |   StdDev |
|-------------------------------------------------------------------- |---------:|--------:|---------:|
| Normal | 183.4 ms | 2.87 ms |  2.40 ms |
| Compressed | 421.9 ms | 8.25 ms | 13.32 ms |
| Encrypted | 260.9 ms | 5.18 ms | 12.41 ms |
| Compressed / Encrypted | 517.2 ms | 9.12 ms |  8.08 ms |


The following is a class diagram lays down some of library components.

![alt tag](https://github.com/royben/Resonance/blob/dev/visuals/Resonance.png)
