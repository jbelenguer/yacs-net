# ![yacs icon](./yacs_64.png) Yacs.NET

[![NuGet version (Yacs.NET)](https://img.shields.io/nuget/v/yacs?style=flat-square)](https://www.nuget.org/packages/Yacs/)

Yet another communication system for .NET

## Introduction

Yacs is an abstraction over the `TcpClient` class from .NET designed to make communication between different application nodes simple.

```cs
using Yacs;

// Create a new Yacs hub listening on port 1234.
var hub = new Hub(1234);

// Create a new Yacs channel to connect to the hub.
var channel = new Channel("127.0.0.1", 1234);

// Send a message.
channel.Send("Hello, this is Yacs.");
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

### Hub

A Yacs hub can be used to accept channel connections, and can be instantiated as follows:

```cs
var hub = new Hub(port);
```

This will start your new hub, listening on the indicated port. What you can do next is subscribe to the event that is raised when a channel connects to the hub:

```cs
hub.ChannelConnected += Hub_ChannelConnected;
```

The event arguments will contain an `IChannel` instance, a reference to which can be kept and used in the same way as if it was created directly:

```cs
private void Hub_ChannelConnected(object sender, ChannelConnectedEventArgs e)
{
   _channels.Add(e.Channel);
}
```

Because `IChannel` implements `IDisposable`, remember to dispose of any channels you receive from the hub once you no longer need to communicate through them.

### Channel

To create a channel and send some data to a hub, it is as easy as:

```cs
var channel = new Channel(host, port);
client.Send("TEST #1");
```

Capturing replies from hubs is also quite easy, just subscribe to the event:

```cs
channel.StringMessageReceived += Client_StringMessageReceived;
```

Remember that using a channel you've created yourself is exactly the same as using a channel provided by a hub.

## Attributions
Icon made by <a href="https://www.flaticon.com/authors/srip" title="srip">srip</a> from <a href="https://www.flaticon.com/" title="Flaticon">www.flaticon.com</a>
