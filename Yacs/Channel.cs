using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Yacs.Events;
using Yacs.Exceptions;
using Yacs.Options;
using Yacs.Services;

namespace Yacs
{
    /// <inheritdoc cref="IChannel" />
    public class Channel : IChannel, IDisposable
    {
        private const int DEFAULT_DELAY = 100;

        private readonly TcpClient _tcpClient;
        private readonly CancellationTokenSource _source = new CancellationTokenSource();
        private readonly Task _messageReceptionTask;
        private readonly ChannelOptions _options;
        private readonly Decoder _decoder;
        private readonly Protocol _protocol;
        private bool _disposedValue;

        /// <inheritdoc/>
        public ChannelIdentifier Identifier { get; }

        internal Channel(TcpClient tcpClient, ChannelOptions options)
        {
            OptionsValidator.Validate(options);

            Identifier = new ChannelIdentifier(tcpClient.Client.RemoteEndPoint);
            _options = options;
            _decoder = _options.Encoding?.GetDecoder();
            _tcpClient = tcpClient;
            _messageReceptionTask = Task.Run(ReceptionLoop, _source.Token);
            _protocol = new Protocol(_options.MaxMessageSize);
        }

        /// <summary>
        /// Creates a new Yacs communication channel for a client.
        /// </summary>
        /// <param name="host">The host to connect to.</param>
        /// <param name="port">The TCP port number to connect to.</param>
        /// <param name="options">The <see cref="ChannelOptions"/> to use to initialise the channel.</param>
        public Channel(string host, int port, ChannelOptions options = null)
            : this(new TcpClient(host, port), options ?? new ChannelOptions())
        {

        }

        /// <summary>
        /// Broadcasts a discovery packet to the entire network, using the specified port.
        /// </summary>
        /// <param name="broadcastPort">The port number to broadcast to.</param>
        /// <param name="timeout">The maximum time to wait for an answer in milliseconds.</param>
        public static async Task<IPEndPoint> Discover(int broadcastPort, int timeout = 10000)
        {
            try
            {
                var discoveryAgent = new UdpClient
                {
                    EnableBroadcast = true
                };
                var discoverPacket = Protocol.CreateDiscoveryRequestMessage();
                await discoveryAgent.SendAsync(discoverPacket, discoverPacket.Length, new IPEndPoint(IPAddress.Broadcast, broadcastPort));

                var responseTask = discoveryAgent.ReceiveAsync();
                if (await Task.WhenAny(responseTask, Task.Delay(timeout)) == responseTask)
                {
                    var serverResponse = await responseTask;
                    serverResponse.RemoteEndPoint.Port = Protocol.DiscoveryResponseReceived(serverResponse.Buffer);
                    discoveryAgent.Close();

                    return serverResponse.RemoteEndPoint;
                }
                else
                {
                    return new IPEndPoint(IPAddress.None, 0);
                }
            }
            catch (Exception)
            {
                return new IPEndPoint(IPAddress.None, 0);
            }
        }

        /// <inheritdoc />
        public void Send(string message)
        {
            if (string.IsNullOrEmpty(message))
                return;
            if (_options.Encoding == null)
            {
                throw new InvalidOperationException($"The channel has no configured encoder, so only bytes can be sent. See {nameof(BaseOptions)}.{nameof(BaseOptions.Encoding)} for more information.");
            }
            var byteArrayMessage = _options.Encoding.GetBytes(message);
            try
            {
                var protocolMessage = Protocol.CreateDataMessage(byteArrayMessage);
                var stream = _tcpClient.GetStream();
                stream.Write(protocolMessage, 0, protocolMessage.Length);
            }
            catch (Exception e)
            {
                _source.Cancel();
                throw new SendMessageException(Identifier, e);
            }
        }

        /// <inheritdoc />
        public void Send(byte[] message)
        {
            if (message == null || message.Length == 0)
                return;
            if (_options.Encoding != null)
            {
                throw new InvalidOperationException($"The channel has a configured encoder, so only strings can be sent. See {nameof(BaseOptions)}.{nameof(BaseOptions.Encoding)} for more information.");
            }
            try
            {
                var protocolMessage = Protocol.CreateDataMessage(message);
                var stream = _tcpClient.GetStream();
                stream.Write(protocolMessage, 0, protocolMessage.Length);
            }
            catch (Exception e)
            {
                _source.Cancel();
                throw new SendMessageException(Identifier, e);
            }
        }

        /// <inheritdoc />
        public event EventHandler<StringMessageReceivedEventArgs> StringMessageReceived;

        /// <inheritdoc />
        public event EventHandler<ByteMessageReceivedEventArgs> ByteMessageReceived;

        /// <inheritdoc />
        public event EventHandler<ChannelDisconnectedEventArgs> Disconnected;

        /// <summary>
        /// Disposes the communication channel and releases all resources used by it.
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the communication channel and releases all resources used by it.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    try
                    {
                        _source?.Cancel();
                    }
                    catch (Exception) { }
                    _tcpClient?.Close();
                    _source?.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                _disposedValue = true;
            }
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

        /// <summary>
        /// Triggers a <see cref="Disconnected"/> event.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        protected virtual void OnDisconnected(ChannelDisconnectedEventArgs e)
        {
            Disconnected?.Invoke(this, e);
        }

        private void ReceptionLoop()
        {
            try
            {
                // Buffer for reading data
                byte[] buffer = new byte[_options.ReceptionBufferSize];
                int bytesRead;

                // Get a stream object for reading and writing
                NetworkStream stream = _tcpClient.GetStream();
                _protocol.ProtocolMessageReceived += Protocol_MessageReceived;

                while (true)
                {
                    if (stream.DataAvailable)
                    {
                        int offset = 0;

                        // Loop to receive all the data sent by the client.
                        while (stream.DataAvailable)
                        {
                            bytesRead = stream.Read(buffer, offset, buffer.Length - offset);
                            offset += bytesRead;
                            if (offset >= buffer.Length)
                            {
                                break;
                            }
                        }

                        _protocol.DataReceived(buffer, offset);
                    }
                    else if (_options.ActiveMonitoring && _tcpClient.Client.Poll(0, SelectMode.SelectRead))
                    {
                        // If the poll returns true, it can be for 3 reasons:
                        // - if Listen() has been called and a connection is pending (we know it is not the case here)
                        // - if data is available for reading (we are going to check now)
                        // - if the connection has been closed, reset, or terminated

                        if (_tcpClient.Client.Available == 0)
                            break;
                    }
                    if (_source.Token.IsCancellationRequested)
                    {
                        break;
                    }

                    Thread.Sleep(DEFAULT_DELAY);
                }
                OnDisconnected(new ChannelDisconnectedEventArgs(Identifier));
            }
            catch (Exception e)
            {
                OnDisconnected(new ChannelDisconnectedEventArgs(Identifier, e));
            }
        }

        private void Protocol_MessageReceived(object sender, ProtocolMessageReceivedEventArgs e)
        {
            if (_options.Encoding == null)
            {
                var byteMessage = new byte[e.Message.Length];
                Array.Copy(e.Message, byteMessage, e.Message.Length);
                OnByteMessageReceived(new ByteMessageReceivedEventArgs(Identifier, byteMessage));
            }
            else
            {
                var decodeableCharacters = _decoder.GetCharCount(e.Message, 0, e.Message.Length, false);
                if (decodeableCharacters > 0)
                {
                    var charPayload = new char[decodeableCharacters];
                    var decodedChars = _decoder.GetChars(e.Message, 0, e.Message.Length, charPayload, 0, false);
                    if (decodedChars > 0)
                    {
                        OnStringMessageReceived(new StringMessageReceivedEventArgs(Identifier, new string(charPayload)));
                    }
                }
            }
        }
    }
}
