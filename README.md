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
