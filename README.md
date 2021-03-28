<p align="center">
  <img src="https://github.com/royben/Resonance/blob/dev/visuals/Logo.png" />    
</p>

# Resonance <img width="20" height="20" src="https://github.com/royben/Resonance/blob/dev/visuals/icon.png" /> [![Build Status](https://sirilix.visualstudio.com/Resonance/_apis/build/status/royben.Resonance?branchName=main)](https://sirilix.visualstudio.com/Resonance/_build/latest?definitionId=1&branchName=main) ![Issues](https://img.shields.io/github/issues/royben/Resonance.svg)

Resonance is a high performance real-time C# communication library with built-in support for several different transcoding and delivery methods.
This library provides an intuitive API for asynchronous communication between machines and devices by exposing a set of easy to use, pluggable components.

The resonance library might be described by the folowing layers:

### Transporting
A transporter responsibility is to provide the API for sending and receving messages, managing those messages and propegate the neccesary information to other components.

### Transcoding
Encoders/Decoders are components that can be pluged to a transporter, they determine how outgoing/incoming messages should be encoded/decoded and whether the data should be encrypted and/or compressed.

### Adapters
Adapters can also be pluged to a transporter to determine how outgoing/incoming encoded data is going to be transmitted and where.


![alt tag](https://github.com/royben/Resonance/blob/dev/visuals/Resonance_Flow.png)
