using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Yacs.Events;
using Yacs.Exceptions;
using Yacs.MessageModels;
using Yacs.Options;
using Yacs.Services;

namespace Yacs
{
    /// <inheritdoc cref="IServer" />
    public class Server : IServer, IDisposable
    {
        private const int DEFAULT_DELAY = 250;

        private readonly int _port;
        private readonly TcpListener _tcpServer;
        private readonly ConcurrentDictionary<ChannelIdentifier, Channel> _knownClients;
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
        public IReadOnlyList<ChannelIdentifier> Channels => _knownClients.Keys.ToList();

        /// <inheritdoc />
        public bool IsDiscoveryEnabled { get; set; }

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

            OptionsValidator.Validate(_options);

            _tcpServer = new TcpListener(IPAddress.Any, port);

            _knownClients = new ConcurrentDictionary<ChannelIdentifier, Channel>();
            _discoveryCancellationSource = new CancellationTokenSource();
            _newClientsCancellationSource = new CancellationTokenSource();

            _newChannelOptions = new ChannelOptions
            {
                Encoding = _options.Encoding,
                ReceptionBufferSize = _options.ReceptionBufferSize,
                ActiveMonitoring = _options.ActiveChannelMonitoring
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
            if (string.IsNullOrEmpty(message))
                return;
            if (_options.Encoding == null)
            {
                throw new InvalidOperationException($"The channel has no configured encoder, so only bytes can be sent. See {nameof(BaseOptions)}.{nameof(BaseOptions.Encoding)} for more information.");
            }

            if (_knownClients.TryGetValue(destination, out var channel))
            {
                channel?.Send(message);
            }
            else
            {
                throw new DisconnectedChannelException(destination);
            }  
        }

        /// <inheritdoc />
        public void Send(ChannelIdentifier destination, byte[] message)
        {
            if (message == null || message.Length == 0)
                return;
            if (_options.Encoding != null)
            {
                throw new InvalidOperationException($"The channel has a configured encoder, so only strings can be sent. See {nameof(BaseOptions)}.{nameof(BaseOptions.Encoding)} for more information.");
            }

            if (_knownClients.TryGetValue(destination, out var channel))
            {
                channel?.Send(message);
            }
            else
            {
                throw new DisconnectedChannelException(destination);
            }
        }

        /// <inheritdoc />
        public bool IsChannelConnected(ChannelIdentifier channel)
        {
            return _knownClients.TryGetValue(channel, out _);
        }

        /// <inheritdoc />
        public void Disconnect(ChannelIdentifier channelId)
        {
            if (_knownClients.TryRemove(channelId, out var channel))
            {
                channel.Dispose();
            }
        }

        /// <inheritdoc />
        public event EventHandler<DiscoveryRequestReceivedEventArgs> DiscoveryRequestReceived;

        /// <inheritdoc />
        public event EventHandler<ChannelConnectedEventArgs> ChannelConnected;

        /// <inheritdoc />
        public event EventHandler<ChannelDisconnectedEventArgs> ChannelDisconnected;

        /// <inheritdoc />
        public event EventHandler<ChannelRefusedEventArgs> ChannelRefused;

        /// <inheritdoc />
        public event EventHandler<StringMessageReceivedEventArgs> StringMessageReceived;

        /// <inheritdoc />
        public event EventHandler<ByteMessageReceivedEventArgs> ByteMessageReceived;

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
        /// Triggers a <see cref="ChannelConnected"/> event.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        protected virtual void OnChannelConnected(ChannelConnectedEventArgs e)
        {
            ChannelConnected?.Invoke(this, e);
        }

        /// <summary>
        /// Triggers a <see cref="DiscoveryRequestReceived"/> event.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        protected virtual void OnDiscoveryRequestReceived(DiscoveryRequestReceivedEventArgs e)
        {
            DiscoveryRequestReceived?.Invoke(this, e);
        }

        /// <summary>
        /// Triggers a <see cref="ChannelDisconnected"/> event.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        protected virtual void OnChannelDisconnected(ChannelDisconnectedEventArgs e)
        {
            ChannelDisconnected?.Invoke(this, e);
        }

        /// <summary>
        /// Triggers a <see cref="ChannelRefused"/> event.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        protected virtual void OnChannelRefused(ChannelRefusedEventArgs e)
        {
            ChannelRefused?.Invoke(this, e);
        }

        /// <summary>
        /// Triggers a <see cref="StringMessageReceived"/> event.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        protected virtual void OnStringMessageReceived(StringMessageReceivedEventArgs e)
        {
            StringMessageReceived?.Invoke(this, e);
        }

        /// <summary>
        /// Triggers a <see cref="ByteMessageReceived"/> event.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        protected virtual void OnByteMessageReceived(ByteMessageReceivedEventArgs e)
        {
            ByteMessageReceived?.Invoke(this, e);
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

                        if (_options.MaximumChannels == 0 || _knownClients.Count < _options.MaximumChannels)
                        {
                            newChannel = new Channel(tcpClient, _newChannelOptions);
                            newChannel.Disconnected += Channel_Disconnected;
                            if (_options.Encoding == null)
                            {
                                newChannel.ByteMessageReceived += Channel_ByteMessageReceived;
                            }
                            else
                            {
                                newChannel.StringMessageReceived += Channel_StringMessageReceived;
                            }

                            if (_knownClients.ContainsKey(newChannel.Identifier))
                            {
                                _knownClients[newChannel.Identifier].Dispose();
                                _knownClients[newChannel.Identifier] = newChannel;
                            }
                            else
                            {
                                _knownClients.TryAdd(new ChannelIdentifier(tcpClient.Client.RemoteEndPoint), newChannel);
                            }
                        }
                        else
                        {
                            var connectionRefusedEventArgs = new ChannelRefusedEventArgs(new ChannelIdentifier(tcpClient.Client.RemoteEndPoint));
                            tcpClient.Close();
                            OnChannelRefused(connectionRefusedEventArgs);
                        }
                        
                        if (newChannel != null)
                        {
                            var connectionReceivedEventArgs = new ChannelConnectedEventArgs(newChannel.Identifier);
                            OnChannelConnected(connectionReceivedEventArgs);
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

                            var discoveryRequestEventArgs = new DiscoveryRequestReceivedEventArgs(RemoteIpEndPoint.ToString());
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

        private void Channel_Disconnected(object sender, ChannelDisconnectedEventArgs e)
        {
            if (_knownClients.TryRemove(e.ChannelIdentifier, out var channel))
            {
                channel.Dispose();
            }
            
            OnChannelDisconnected(e);
        }

        private void Channel_StringMessageReceived(object sender, StringMessageReceivedEventArgs e)
        {
            OnStringMessageReceived(e);
        }

        private void Channel_ByteMessageReceived(object sender, ByteMessageReceivedEventArgs e)
        {
            OnByteMessageReceived(e);
        }
    }
}
