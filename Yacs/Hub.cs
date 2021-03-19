using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Yacs.Events;
using Yacs.Exceptions;
using Yacs.Options;
using Yacs.Services;

namespace Yacs
{
    /// <inheritdoc cref="IHub" />
    public class Hub : IHub, IDisposable
    {
        private const int DEFAULT_DELAY = 250;

        private readonly int _port;
        private readonly TcpListener _tcpServer;
        private readonly UdpClient _udpServer;
        private readonly ChannelOptions _newChannelOptions;
        private readonly HubOptions _options;

        private readonly Task _newConnectionsTask;
        private readonly Task _discoveryTask;

        private bool _shouldContinueDiscoveryTask;
        private bool _shouldContinueNewConnectionsTask;
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

        /// <summary>
        /// Creates a new Yacs <see cref="Hub"/>. This can accept connections from many <see cref="Channel"/> instances.
        /// </summary>
        /// <param name="port">The port on which the server will be listening.</param>
        /// <param name="options">The server options.</param>
        public Hub(int port, HubOptions options = null)
        {
            _port = port;
            _options = options
                ?? new HubOptions();

            OptionsValidator.Validate(_options);

            _tcpServer = new TcpListener(IPAddress.Any, port);

            _shouldContinueDiscoveryTask = true;
            _shouldContinueNewConnectionsTask = true;

            _newChannelOptions = new ChannelOptions
            {
                Encoding = _options.Encoding,
                ReceptionBufferSize = _options.ReceptionBufferSize,
                ActiveMonitoring = _options.ActiveChannelMonitoring
            };

            if (_options.IsDiscoverable)
            {
                IsDiscoveryEnabled = true;
                _udpServer = new UdpClient(_options.DiscoveryPort);
                _discoveryTask = Task.Run(DiscoveryLoop);
            }

            _enabled = true;
            _tcpServer.Start();
            _newConnectionsTask = Task.Run(NewConnectionListenerLoop);
        }

        /// <inheritdoc />
        public event EventHandler<DiscoveryRequestReceivedEventArgs> DiscoveryRequestReceived;

        /// <inheritdoc />
        public event EventHandler<ChannelConnectedEventArgs> ChannelConnected;

        /// <summary>
        /// Stops the <see cref="Hub"/> in an ordered manner, releasing all the resources used by it.
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Stops the <see cref="Hub"/> in an ordered manner, releasing all the resources used by it.
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
                    _shouldContinueDiscoveryTask = false;
                    _shouldContinueNewConnectionsTask = false;
                    _udpServer?.Close();
                    _udpServer?.Dispose();
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

        private async Task NewConnectionListenerLoop()
        {
            try
            {
                while (_shouldContinueNewConnectionsTask)
                {
                    // Perform a blocking call to accept requests.
                    TcpClient tcpClient = await _tcpServer.AcceptTcpClientAsync();

                    if (_enabled && ChannelConnected != null)
                    {
                        var newChannel = new Channel(tcpClient, _newChannelOptions);

                        var connectionReceivedEventArgs = new ChannelConnectedEventArgs(newChannel);
                        OnChannelConnected(connectionReceivedEventArgs);
                    }
                    else
                    {
                        tcpClient.Close();
                    }
                    await Task.Delay(DEFAULT_DELAY);
                }
            }
            catch (InvalidOperationException e)
            {
                throw new HubFailureException($"The {nameof(Hub)} found a problem and it is going to stop. Error was: {e.Message}", e);
            }
            catch (SocketException e)
            {
                throw new HubFailureException($"The {nameof(Hub)} found a network problem and it is going to stop. Error was: {e.Message}", e);
            }
        }

        private async Task DiscoveryLoop()
        {
            try
            {
                while (_shouldContinueDiscoveryTask)
                {
                    try
                    {
                        // Blocks until a message returns on this socket from a remote host.
                        var udpReceiveResult = await _udpServer.ReceiveAsync();

                        if (IsDiscoveryEnabled && Protocol.ValidateDiscoveryRequest(udpReceiveResult.Buffer))
                        {
                            var response = Protocol.CreateDiscoveryResponseMessage(_port);
                            await _udpServer.SendAsync(response, response.Length, udpReceiveResult.RemoteEndPoint);

                            var discoveryRequestEventArgs = new DiscoveryRequestReceivedEventArgs(udpReceiveResult.RemoteEndPoint.ToString());
                            OnDiscoveryRequestReceived(discoveryRequestEventArgs);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                    }
                    await Task.Delay(DEFAULT_DELAY);
                }
            }
            catch (Exception e)
            {
                throw new HubFailureException($"The {nameof(Hub)} found an unrecoverable problem with the discovery system and it has been disabled. {nameof(Hub)} should still work, but channels are not going to be able to discover this {nameof(Hub)} anymore. Error was: {e.Message}", e);
            }
        }
    }
}
