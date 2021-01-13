# ![yacs icon](./yacs_64.png) Yacs.NET

[![NuGet version (Yacs.NET)](https://img.shields.io/nuget/v/yacs?style=flat-square)](https://www.nuget.org/packages/Yacs/)

Yet another communication system for .NET

## Introduction

Yacs is an abstraction over the `TcpClient` class from .NET designed to make communication between different application nodes simple.

```cs
using Yacs;

// Create a new Yacs server listening on port 1234.
var server = new Server(1234);

// Create a new Yacs channel to connect to the server.
var channel = new Channel("127.0.0.1", 1234);

// Send a message.
channel.Send("Hello, this Yacs.");
```

## Motivation

Yacs:

- Allows an application to communicate directly with other nodes via TCP
- Notifies applications of incoming messages and losses of connection via standard .NET events
- Supports sending and receiving messages as strings or byte arrays

Yacs has a few advanced features, such as node discovery for local networks, but it is not intended to act as a complex communication framework – Yacs' purpose is to do just enough to save you from the tedious and repetitive work normally involved in using TCP in .NET.

## Basic usage

For basic usage you only need the classes in the namespace `Yacs`. So: 

```cs
using Yacs;
```

### Server

A Yacs server can be instantiated as follows:

```cs
var server = new Server(port);
```

This will start your new server, listening on the indicated port. This by itself is not very useful, so what you can do next is subscribe to the events you are interested in. For instance, you could subscribe to new messages received with:

```cs
server.StringMessageReceived += Server_StringMessageReceived;
```

Every event will identify the channel that triggered it, so it is easy to implement a reply with something like the following:

```cs
private static void MyServer_StringMessageReceived(object sender, StringMessageReceivedEventArgs e)
{
    _server.Send(e.ChannelIdentifier, "Ok, copy!");
}
```

### Channel

To create a channel and send some data to the server, it is as easy as:

```cs
var channel = new Channel(host, port);
client.Send("TEST #1");
```

Capturing replies from servers is also quite easy, just subscribe to the event:

```cs
channel.StringMessageReceived += Client_StringMessageReceived;
```

## Attributions
Icon made by <a href="https://www.flaticon.com/authors/srip" title="srip">srip</a> from <a href="https://www.flaticon.com/" title="Flaticon">www.flaticon.com</a>
