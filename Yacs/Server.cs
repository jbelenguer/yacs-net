using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Yacs.Events;
using Yacs.Exceptions;
using Yacs.MessageModels;
using Yacs.Options;

namespace Yacs
{
    /// <summary>
    /// Represents a Yacs server. A server is able to accept many connections from many channels.
    /// </summary>
    public class Server : IDisposable
    {
        private const int DEFAULT_DELAY = 250;
        private readonly object _channelsLock = new object();

        private readonly int _port;
        private readonly TcpListener _tcpServer;
        private readonly Dictionary<EndPoint, Channel> _knownClients;
        private readonly CancellationTokenSource _discoveryCancellationSource;
        private readonly CancellationTokenSource _newClientsCancellationSource;
        private readonly ChannelOptions _newChannelOptions;
        private readonly ServerOptions _options;

        private readonly Task _newConnectionsTask;

        private Task _discoveryTask;
        private UdpClient _discoveryAgent;

        private bool disposedValue;
        private bool _enabled = false;

        /// <summary>
        /// Enables or disables the new connections. If the <see cref="Server"/> is discoverable, the flag also affects the discovery feature. 
        /// NOTE: This means that if you re-enable the <see cref="Server"/>, it would re-enable the discovery, no matter what its value was before. 
        /// </summary>
        public bool Enabled
        {
            get
            {
                return _enabled;
            }
            set
            {
                _enabled = value;
                DiscoveryEnabled = _enabled && _options.IsDiscoverable;
            }
        }

        /// <summary>
        /// If the feature was enabled on creation, this flag enables or disables the discovery.
        /// </summary>
        public bool DiscoveryEnabled { get; set; }

        public int ChannelsCount
        {
            get { lock (_channelsLock) { return _knownClients.Count; } }
        }

        /// <summary>
        /// Creates a new Yacs <see cref="Server"/>. Then it can accept connections from many <see cref="Channel"/>.
        /// </summary>
        /// <param name="port">Port in which it will be listening to.</param>
        /// <param name="options">Server options.</param>
        public Server(int port, ServerOptions options = null)
        {
            _port = port;
            _options = options 
                ?? new ServerOptions();

            _tcpServer = new TcpListener(IPAddress.Loopback, port);
            _knownClients = new Dictionary<EndPoint, Channel>();
            _discoveryCancellationSource = new CancellationTokenSource();
            _newClientsCancellationSource = new CancellationTokenSource();

            _newChannelOptions = new ChannelOptions
            {
                Encoder = _options.Encoder,
                ReceptionBufferSize = _options.ReceptionBufferSize
            };

            if (_options.IsDiscoverable)
            {
                DiscoveryEnabled = true;
                _discoveryAgent = new UdpClient(_options.DiscoveryPort);
                _discoveryTask = Task.Run(DiscoveryLoop, _discoveryCancellationSource.Token);
            }

            _enabled = true;
            _tcpServer.Start();
            _newConnectionsTask = Task.Run(NewConnectionListenerLoop, _newClientsCancellationSource.Token);
        }

        /// <summary>
        /// Sends data to a specific <see cref="Channel"/>.
        /// </summary>
        /// <param name="destination"><see cref="Channel"/> end point.</param>
        /// <param name="message">Message to send.</param>
        public void Send(EndPoint destination, string message)
        {
            bool errored = false;
            lock (_channelsLock)
            {
                if (_knownClients.ContainsKey(destination))
                {
                    _knownClients[destination].Send(message);
                }
                else
                {
                    errored = true;
                }
            }
            if (errored)
            {
                OnError(new ChannelErrorEventArgs(destination, new OfflineChannelException(destination)));
            }
        }

        /// <summary>
        /// Gets if a specific <see cref="Channel"/> is online.
        /// </summary>
        /// <param name="channel"></param>
        /// <returns></returns>
        public bool IsChannelOnline(EndPoint channel)
        {
            lock (_channelsLock)
            {
                return _knownClients.ContainsKey(channel);
            }
        }

        /// <summary>
        /// Stops the <see cref="Server"/> in an ordered manner, releasing all the resources used by it.
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Event triggered when a <see cref="Channel"/> starts a connection to the <see cref="Server"/>.
        /// </summary>
        public event EventHandler<ConnectionReceivedEventArgs> ConnectionReceived;
        /// <summary>
        /// Event triggered when a discovery request is received from a <see cref="Channel"/>.
        /// </summary>
        public event EventHandler<DiscoveryRequestEventArgs> DiscoveryRequestReceived;
        /// <summary>
        /// Event triggered when a connection to a <see cref="Channel"/> is lost.
        /// </summary>
        public event EventHandler<ConnectionLostEventArgs> ConnectionLost;
        /// <summary>
        /// Event triggered when a message is received from a <see cref="Channel"/>.
        /// </summary>
        public event EventHandler<MessageReceivedEventArgs> MessageReceived;
        /// <summary>
        /// Event triggered when a <see cref="Channel"/> throws an error.
        /// </summary>
        public event EventHandler<ChannelErrorEventArgs> ChannelError;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _enabled = false;
                    DiscoveryEnabled = false;
                    _discoveryCancellationSource?.Cancel();
                    _newClientsCancellationSource?.Cancel();
                    _discoveryAgent.Close();
                    foreach (var client in _knownClients.Values)
                    {
                        client.Dispose();
                    }
                    _discoveryAgent.Dispose();
                    _discoveryTask.Dispose();
                    _newConnectionsTask.Dispose();
                    _tcpServer.Stop();
                }

                disposedValue = true;
            }
        }

        protected virtual void OnConnectionReceived(ConnectionReceivedEventArgs e)
        {
            ConnectionReceived?.Invoke(this, e);
        }

        protected virtual void OnDiscoveryRequestReceived(DiscoveryRequestEventArgs e)
        {
            DiscoveryRequestReceived?.Invoke(this, e);
        }

        protected virtual void OnConnectionLost(ConnectionLostEventArgs e)
        {
            ConnectionLost?.Invoke(this, e);
        }

        protected virtual void OnMessageReceived(MessageReceivedEventArgs e)
        {
            MessageReceived?.Invoke(this, e);
        }

        protected virtual void OnError(ChannelErrorEventArgs e)
        {
            ChannelError?.Invoke(this, e);
        }

        private void NewConnectionListenerLoop()
        {
            try
            {
                while (true)
                {
                    // Perform a blocking call to accept requests.
                    TcpClient tcpClient = _tcpServer.AcceptTcpClient();

                    if (_enabled)
                    {
                        var newChannel = new Channel(tcpClient, _newChannelOptions);
                        lock (_channelsLock)
                        {
                            if (_options.MaximumChannels > 0 && _knownClients.Count < _options.MaximumChannels)
                            {
                                newChannel.ConnectionLost += Channel_ConnectionLost;
                                newChannel.MessageReceived += Channel_MessageReceived;
                                newChannel.ChannelError += Channel_Error;

                                if (_knownClients.ContainsKey(newChannel.RemoteEndPoint))
                                {
                                    _knownClients[newChannel.RemoteEndPoint].Dispose();
                                    _knownClients[newChannel.RemoteEndPoint] = newChannel;
                                }
                                else
                                {
                                    _knownClients.Add(tcpClient.Client.RemoteEndPoint, newChannel);
                                }
                            }
                        }

                        var connectionReceivedEventArgs = new ConnectionReceivedEventArgs(newChannel.RemoteEndPoint);
                        OnConnectionReceived(connectionReceivedEventArgs);
                    }
                    Task.Delay(DEFAULT_DELAY);
                }
            }
            catch (InvalidOperationException e)
            {
                throw new ServerFailureException($"The server found a problem and it is going to stop. Error was: {e.Message}", e);
            }
            catch (SocketException e)
            {
                throw new ServerFailureException($"The server found a network problem and it is going to stop. Error was: {e.Message}", e);
            }
        }

        private void DiscoveryLoop()
        {
            try
            {
                while (true)
                {
                    IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
                    try
                    {
                        // Blocks until a message returns on this socket from a remote host.
                        byte[] discoveryRequest = _discoveryAgent.Receive(ref RemoteIpEndPoint);

                        if (DiscoveryEnabled && DiscoveryMessage.IsValidRequest(discoveryRequest))
                        {
                            var response = DiscoveryMessage.CreateReply(_port);
                            _discoveryAgent.Send(response, response.Length, RemoteIpEndPoint);

                            var discoveryRequestEventArgs = new DiscoveryRequestEventArgs(RemoteIpEndPoint);
                            OnDiscoveryRequestReceived(discoveryRequestEventArgs);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                    }
                    Task.Delay(DEFAULT_DELAY);
                }
            }
            catch (Exception e)
            {
                throw new ServerFailureException($"The server found an unrecoverable problem with the discovery system and it has been disabled. Server should still work, but channels are not going to be able to discover this server anymore. Error was: {e.Message}", e);
            }
        }

        private void Channel_ConnectionLost(object sender, ConnectionLostEventArgs e)
        {
            lock (_channelsLock)
            {
                if (_knownClients.TryGetValue(e.EndPoint, out var channel))
                {
                    channel.Dispose();
                    lock (_channelsLock)
                    {
                        _knownClients.Remove(e.EndPoint);
                    }
                }
            }
            OnConnectionLost(e);
        }

        private void Channel_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            OnMessageReceived(e);
        }

        private void Channel_Error(object sender, ChannelErrorEventArgs e)
        {
            OnError(e);
        }
        
    }
}
