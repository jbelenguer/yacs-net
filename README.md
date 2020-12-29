# YACS.NET
![yacs icon](https://github.com/jbelenguer/yacs-net/blob/master/yacs_64.png)
Yet another communication system for .NET

## Motivation
It is not the first time that I needed a communication library to allow me:
- Communicate different nodes using TCP directly
- Simple to use
- Have some advanced features (like node discovery)

Either if you are developing a game, or your own IoT system, the only option out there is to actually write it yourself over the System.Net.Sockets library (TcpListener/TcpClient).

Since this is not the first time I find this issue, I decided to write one so I can reuse it in a couple of projects.

## Basic usage
For the basic usage you will need the classes in the namespace `Yacs`. So: 
```cs
using Yacs;
```

### Server
To create a yacs server you just need to do:
```cs
Server _myServer = new Server(port);
```
This will start your new server, listening in the indicated port. This by itself is not very useful, so what you should do next is subscribe to the events you are interested at. For instance, you could subscribe to the new messages received with:
```cs
_myServer.MessageReceived += MyServer_MessageReceived;
```

Every event will identify the client who triggerede it with their end point, so it is easy to implement a reply with something like:
```cs
private static void MyServer_MessageReceived(object sender, SimpleSocket.Events.MessageReceivedEventArgs e)
{
    _myServer.Send(e.EndPoint, "Ok, copy!");
}
```
### Client
To create a client and send some data, it is as easy as:
```cs
client = new Channel(address, port);
client.Send("TEST #1");
```

Capture replies from servers is also quite easy subscribing to the event:
```cs
client.MessageReceived += Client_MessageReceived;
```



## Attributions
Icon made by <a href="https://www.flaticon.com/authors/srip" title="srip">srip</a> from <a href="https://www.flaticon.com/" title="Flaticon">www.flaticon.com</a>
