# Resonance
Resonance is a high-performance real-time C# communication library with built-in support for several different transcoding and delivery methods.
This library provides an intuitive API for asynchronous communication between machines and devices by exposing a set of easy to use, pluggable components.

## Overview
Resonance is a request-response based communication framework.
This means that for each request that is being sent, a matching response is expected.
This is done by attaching a unique token to each request and expecting the same token from the response.
Although the request-response pattern is the recommended approach, it is not enforced. Sending messages without expecting any response is possible.


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
