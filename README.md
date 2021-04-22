<p align="center">
  <img src="https://github.com/royben/Resonance/blob/dev/visuals/Logo.png" />    
</p>

# Resonance <img width="20" height="20" src="https://github.com/royben/Resonance/blob/dev/visuals/icon.png" /> [![Build Status](https://sirilix.visualstudio.com/Resonance/_apis/build/status/royben.Resonance?branchName=main)](https://sirilix.visualstudio.com/Resonance/_build/latest?definitionId=1&branchName=main) ![Issues](https://img.shields.io/github/issues/royben/Resonance.svg)

Resonance is a high-performance real-time C# communication library with built-in support for several different transcoding and delivery methods.
This library provides an intuitive API for asynchronous communication between machines and devices by exposing a set of easy to use, pluggable components.

<br/>
<br/>

| Module | Nuget | Description | Target Framework
|:---------------------------------------------|:---------|:--------|:--------|
| Resonance | ![Nuget](https://img.shields.io/nuget/v/Resonance) | Provides support for TCP, UDP and NamedPipes. | .NET Standard 2.0 |
| Resonance.Protobuf | ![Nuget](https://img.shields.io/nuget/v/Resonance.Protobuf) | Protobuf Encoder & Decoder | .NET Standard 2.0 |
| Resonance.USB | ![Nuget](https://img.shields.io/nuget/v/Resonance.USB) | USB Adapter support.  | .NET 4.6.1, .NET 5 |
| Resonance.SignalR | ![Nuget](https://img.shields.io/nuget/v/Resonance.SignalR) | SignalR (core and legacy) Adapters and Hubs. | .NET 4.6.1, .NET 5 |
| Resonance.WebRTC | ![Nuget](https://img.shields.io/nuget/v/Resonance.WebRTC) | WebRTC Adapter support. | .NET 4.6.1, .NET 5 |

<br/>
<br/>

## Overview
Resonance is a request-response based communication framework.
This means that for each request that is being sent, a matching response is expected.
This is done by attaching a unique token to each request and expecting the same token from the response.
Although the request-response pattern is the recommended approach, it is not enforced. Sending messages without expecting any response is possible.

<br/>

The following diagram provides a basic overview of a message being sent.

![alt tag](https://github.com/royben/Resonance/blob/dev/visuals/Resonance_Protocol.png)

<br/>
<br/>

The resonance library might be described by these 3 basic layers:

### Transporting
A transporter responsibility is to provide the API for sending and receiving messages, managing those messages, and propagating the necessary information to other components.

### Transcoding
Encoders and Decoders are components that can be plugged to a transporter, they determine how outgoing/incoming messages should be encoded and whether the data should be encrypted and/or compressed.
The Following built-in transcoding methods are currently supported by the library:
*	Json
*	Bson
*	Protobuf
*	Xml

### Adapters
Adapters can also be plugged to a transporter to determine how outgoing/incoming encoded data is going to be transmitted and where.
The following built-in adapters are currently supported by the library:
*	TCP
*	UDP
*	USB
*	In-Memory
*	SignalR
*	WebRTC
*	Named Pipes
*	Shared Memory

<br/>

The following diagram described a simple request-response scenario.

![alt tag](https://github.com/royben/Resonance/blob/dev/visuals/Resonance_Flow.png)

<br/>

# Usage Examples

<br/>

#### Create a Transporter and send a message.
```c#
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
```
<br/>

#### Using Fluent Builder.
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

#### Connecting between Transporters using a TCP server.
```c#
        public async void Demo()
        {
            ResonanceTcpServer server = new ResonanceTcpServer(8888);
            server.Start();
            server.ClientConnected += Server_ClientConnected;

            IResonanceTransporter transporter1 = ResonanceTransporter.Builder.Create()
                .WithTcpAdapter()
                .WithAddress("127.0.0.1")
                .WithPort(8888)
                .WithJsonTranscoding()
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
            IResonanceTransporter transporter2 = ResonanceTransporter.Builder.Create()
                .WithTcpAdapter()
                .FromTcpClient(e.TcpClient)
                .WithJsonTranscoding()
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

#### Registering a Request Handler.
```c#
        public async void Demo()
        {
            IResonanceTransporter transporter = ResonanceTransporter.Builder
                .Create().WithTcpAdapter()
                .WithAddress("127.0.0.1")
                .WithPort(8888)
                .WithJsonTranscoding()
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

# Services
The Transporter also supports registering a service instance as an easy request handling mechanism.

#### Registering a Resonance Service.
```c#
        public async void Demo()
        {
            IResonanceTransporter transporter = ResonanceTransporter.Builder
                .Create()
                .WithTcpAdapter()
                .WithAddress("127.0.0.1")
                .WithPort(8888)
                .WithJsonTranscoding()
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
```

<br/>

# In-Memory Testing
Communication testing can easily be done using the **InMemory** adapter.
Notice how both transporters are using the In-Memory adapter with the same address.

```c#
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
```

<br/>

# Continuous Response
The Resonance library supports the concept of a continuous response where one transporter sends a single request
while expecting multiple response messages. This method works best when you want to report about some progress being made,
or to send large amount of data with less overhead.

Sending a continuous request can be done using the **SendContinuousRequest** method and providing an **observer**.

```c#
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
```

<br/>

# Error Handling
The Resonance library supports an automatic error handling mechanism which makes it easy to report and handle errors.
The following example demonstrate how to report an error from one side while handling it on the other.

```c#
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
```
<br/>

# Logging
The Resonance library takes advantage of structured logs and makes it easy to track the full path of each request.
You can easily trace all communication using your favorite logging library by providing an instance of ILoggingFactory.

**Hooking Resonance to Serilog**

```c#
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
```
<br/>

**Specifying logging degree per request**

<br/>

```c#
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
```

<br/>

**Viewing and tracking a request using Seq and the request Token property.**
![alt tag](https://github.com/royben/Resonance/blob/dev/visuals/Seq.png)

<br/>
<br/>
<br/>

# Benchmarks
>1000 Roundtrips (request -> response), Intel Core i7-6700HQ CPU 2.60GHz (Skylake)

**Transcoding**

|                                       Method |     Mean |   Error |  StdDev |
|--------------------------------------------- |---------:|--------:|--------:|
| Json | 205.6 ms | 4.09 ms | 9.96 ms |
| Protobuf | 180.3 ms | 4.07 ms | 3.81 ms |

<br/>

**Encryption / Compression**

|                                                              Method |     Mean |   Error |   StdDev |
|-------------------------------------------------------------------- |---------:|--------:|---------:|
| Normal | 183.4 ms | 2.87 ms |  2.40 ms |
| Compressed | 421.9 ms | 8.25 ms | 13.32 ms |
| Encrypted | 260.9 ms | 5.18 ms | 12.41 ms |
| Compressed / Encrypted | 517.2 ms | 9.12 ms |  8.08 ms |
