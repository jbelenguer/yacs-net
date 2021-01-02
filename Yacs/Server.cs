using System;
using System.Collections.Generic;
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
    /// <inheritdoc cref="IServer" />
    public class Server : IServer, IDisposable
    {
        private const int DEFAULT_DELAY = 250;
        private readonly object _channelsLock = new object();

        private readonly int _port;
        private readonly TcpListener _tcpServer;
        private readonly Dictionary<ChannelIdentifier, Channel> _knownClients;
        private readonly CancellationTokenSource _discoveryCancellationSource;
        private readonly CancellationTokenSource _newClientsCancellationSource;
        private readonly ChannelOptions _newChannelOptions;
        private readonly ServerOptions _options;

        private readonly Task _newConnectionsTask;

        private readonly Task _discoveryTask;
        private readonly UdpClient _discoveryAgent;

        private bool disposedValue;
        private bool _enabled = false;

        /// <inheritdoc />
        public bool IsEnabled
        {
            get { return _enabled; }
            set
            {
                _enabled = value;
                IsDiscoveryEnabled = _enabled && _options.IsDiscoverable;
            }
        }

        /// <inheritdoc />
        public bool IsDiscoveryEnabled { get; set; }

        /// <inheritdoc />
        public int ChannelCount
        {
            get { lock (_channelsLock) { return _knownClients.Count; } }
        }

        /// <summary>
        /// Creates a new Yacs <see cref="Server"/>. This can accept connections from many <see cref="Channel"/> instances.
        /// </summary>
        /// <param name="port">The port on which the server will be listening.</param>
        /// <param name="options">The server options.</param>
        public Server(int port, ServerOptions options = null)
        {
            _port = port;
            _options = options
                ?? new ServerOptions();

            _tcpServer = new TcpListener(IPAddress.Loopback, port);
            _knownClients = new Dictionary<ChannelIdentifier, Channel>();
            _discoveryCancellationSource = new CancellationTokenSource();
            _newClientsCancellationSource = new CancellationTokenSource();

            _newChannelOptions = new ChannelOptions
            {
                Encoder = _options.Encoder,
                ReceptionBufferSize = _options.ReceptionBufferSize,
                KeepAlive = _options.ActiveChannelMonitoring
            };

            if (_options.IsDiscoverable)
            {
                IsDiscoveryEnabled = true;
                _discoveryAgent = new UdpClient(_options.DiscoveryPort);
                _discoveryTask = Task.Run(DiscoveryLoop, _discoveryCancellationSource.Token);
            }

            _enabled = true;
            _tcpServer.Start();
            _newConnectionsTask = Task.Run(NewConnectionListenerLoop, _newClientsCancellationSource.Token);
        }

        /// <inheritdoc />
        public void Send(ChannelIdentifier destination, string message)
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

        /// <inheritdoc />
        public bool IsChannelOnline(ChannelIdentifier channel)
        {
            lock (_channelsLock)
            {
                return _knownClients.ContainsKey(channel);
            }
        }

        /// <inheritdoc />
        public void Disconnect(ChannelIdentifier channel)
        {
            bool errored = false;
            lock (_channelsLock)
            {
                if (_knownClients.ContainsKey(channel))
                {
                    _knownClients[channel].Dispose();
                }
                else
                {
                    errored = true;
                }
                _knownClients.Remove(channel);
            }

            if (errored)
            {
                OnError(new ChannelErrorEventArgs(channel, new OfflineChannelException(channel)));
            }
        }

        /// <inheritdoc />
        public event EventHandler<ConnectionReceivedEventArgs> ConnectionReceived;

        /// <inheritdoc />
        public event EventHandler<DiscoveryRequestEventArgs> DiscoveryRequestReceived;

        /// <inheritdoc />
        public event EventHandler<ConnectionLostEventArgs> ConnectionLost;

        /// <inheritdoc />
        public event EventHandler<MessageReceivedEventArgs> MessageReceived;

        /// <inheritdoc />
        public event EventHandler<ChannelErrorEventArgs> ChannelError;

        /// <summary>
        /// Stops the <see cref="Server"/> in an ordered manner, releasing all the resources used by it.
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Stops the <see cref="Server"/> in an ordered manner, releasing all the resources used by it.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _enabled = false;
                    IsDiscoveryEnabled = false;
                    _discoveryCancellationSource?.Cancel();
                    _newClientsCancellationSource?.Cancel();
                    _discoveryAgent.Close();
                    foreach (var client in _knownClients.Values)
                    {
                        client.Dispose();
                    }
                    _discoveryAgent.Dispose();
                    _tcpServer.Stop();
                }

                disposedValue = true;
            }
        }

        /// <summary>
        /// Triggers a <see cref="ConnectionReceived"/> event.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        protected virtual void OnConnectionReceived(ConnectionReceivedEventArgs e)
        {
            ConnectionReceived?.Invoke(this, e);
        }

        /// <summary>
        /// Triggers a <see cref="DiscoveryRequestReceived"/> event.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        protected virtual void OnDiscoveryRequestReceived(DiscoveryRequestEventArgs e)
        {
            DiscoveryRequestReceived?.Invoke(this, e);
        }

        /// <summary>
        /// Triggers a <see cref="ConnectionLost"/> event.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        protected virtual void OnConnectionLost(ConnectionLostEventArgs e)
        {
            ConnectionLost?.Invoke(this, e);
        }

        /// <summary>
        /// Triggers a <see cref="MessageReceived"/> event.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        protected virtual void OnMessageReceived(MessageReceivedEventArgs e)
        {
            MessageReceived?.Invoke(this, e);
        }

        /// <summary>
        /// Triggers a <see cref="ChannelError"/> event.
        /// </summary>
        /// <param name="e">Event arguments.</param>
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
                        Channel newChannel = null;
                        lock (_channelsLock)
                        {
                            if (_options.MaximumChannels == 0 || _knownClients.Count < _options.MaximumChannels)
                            {
                                newChannel = new Channel(tcpClient, _newChannelOptions);
                                newChannel.ConnectionLost += Channel_ConnectionLost;
                                newChannel.MessageReceived += Channel_MessageReceived;
                                newChannel.ChannelError += Channel_Error;

                                if (_knownClients.ContainsKey(newChannel.Identifier))
                                {
                                    _knownClients[newChannel.Identifier].Dispose();
                                    _knownClients[newChannel.Identifier] = newChannel;
                                }
                                else
                                {
                                    _knownClients.Add(new ChannelIdentifier(tcpClient.Client.RemoteEndPoint), newChannel);
                                }
                            }
                            else
                            {
                                var connectionLostEventArgs = new ConnectionLostEventArgs(new ChannelIdentifier(tcpClient.Client.RemoteEndPoint), "Connection refused. The number of active connections has reached the limit.");
                                tcpClient.Close();
                                OnConnectionLost(connectionLostEventArgs);
                            }
                        }
                        if (newChannel != null)
                        {
                            var connectionReceivedEventArgs = new ConnectionReceivedEventArgs(newChannel.Identifier);
                            OnConnectionReceived(connectionReceivedEventArgs);
                        }
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

                        if (IsDiscoveryEnabled && DiscoveryMessage.IsValidRequest(discoveryRequest))
                        {
                            var response = DiscoveryMessage.CreateReply(_port);
                            _discoveryAgent.Send(response, response.Length, RemoteIpEndPoint);

                            var discoveryRequestEventArgs = new DiscoveryRequestEventArgs(RemoteIpEndPoint.ToString());
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
                    _knownClients.Remove(e.EndPoint);
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
