using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Yacs.Events;
using Yacs.MessageModels;
using Yacs.Options;

namespace Yacs
{
    /// <summary>
    /// Represents a communication channel.
    /// </summary>
    public class Channel : IDisposable
    {
        private const int DEFAULT_DELAY = 100;

        private readonly TcpClient _tcpClient;
        private readonly CancellationTokenSource _source = new CancellationTokenSource();
        private readonly Task _messageReceptionTask;
        private readonly ChannelOptions _options;
        private bool disposedValue;

        /// <summary>
        /// Gets the end point for this channel.
        /// </summary>
        public ChannelIdentifier RemoteEndPoint { get; private set; }

        internal Channel(TcpClient tcpClient, ChannelOptions options)
        {
            RemoteEndPoint = new ChannelIdentifier(tcpClient.Client.RemoteEndPoint);
            _options = options;
            _tcpClient = tcpClient;
            _messageReceptionTask = Task.Run(ReceptionLoop, _source.Token);
        }

        /// <summary>
        /// Creates a new communication channel for a client. 
        /// </summary>
        /// <param name="serverUrl">Url to connect to.</param>
        /// <param name="port">TCP/UDP port number.</param>
        /// <param name="options"><see cref="ChannelOptions"/> to initialise the channel.</param>
        public Channel(string serverUrl, int port, ChannelOptions options = null) 
            : this(new TcpClient(serverUrl, port), options ?? new ChannelOptions())
        {

        }

        /// <summary>
        /// Broadcasts a discover packet to the entire network, using the specified port. Returns the 
        /// </summary>
        /// <param name="broadcastPort">Port number to braodcast from.</param>
        /// <param name="timeout">Time to wait for an answer in milliseconds.</param>
        public static async Task<IPEndPoint> Discover(int broadcastPort, int timeout = 10000)
        {
            try
            {
                var discoveryAgent = new UdpClient
                {
                    EnableBroadcast = true
                };
                var discoverPacket = DiscoveryMessage.Request;
                await discoveryAgent.SendAsync(discoverPacket, discoverPacket.Length, new IPEndPoint(IPAddress.Broadcast, broadcastPort));

                var responseTask = discoveryAgent.ReceiveAsync();
                if (await Task.WhenAny(responseTask, Task.Delay(timeout)) == responseTask)
                {
                    var serverResponse = await responseTask;
                    serverResponse.RemoteEndPoint.Port = DiscoveryMessage.ParseDiscoveryPort(serverResponse.Buffer);
                    discoveryAgent.Close();

                    return serverResponse.RemoteEndPoint;
                }
                else
                {
                    return new IPEndPoint(IPAddress.None, 0);
                }
            }
            catch(Exception)
            {
                return new IPEndPoint(IPAddress.None, 0);
            }
        }

        /// <summary>
        /// Sends a message in the channel.
        /// </summary>
        /// <param name="message"></param>
        public void Send(string message)
        {
            try
            {
                var msg = _options.Encoder.GetBytes(message);

                var stream = _tcpClient.GetStream();
                stream.Write(msg, 0, msg.Length);
            }
            catch (IOException e)
            {
                OnError(new ChannelErrorEventArgs(RemoteEndPoint, e));
            }
            catch (SocketException e)
            {
                OnError(new ChannelErrorEventArgs(RemoteEndPoint, e));
            }
            catch (ObjectDisposedException e)
            {
                OnConnectionLost(new ConnectionLostEventArgs(RemoteEndPoint, e));
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Event triggered to indicate there is a new message.
        /// </summary>
        public event EventHandler<MessageReceivedEventArgs> MessageReceived;

        /// <summary>
        /// Event triggered indicating a <see cref="Channel"/> connection lost.
        /// </summary>
        public event EventHandler<ConnectionLostEventArgs> ConnectionLost;

        /// <summary>
        /// Event triggered indicating an error.
        /// </summary>
        public event EventHandler<ChannelErrorEventArgs> ChannelError;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _source?.Cancel();
                    _tcpClient?.Close();
                    _source?.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
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
        /// Triggers a <see cref="ConnectionLost"/> event.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        protected virtual void OnConnectionLost(ConnectionLostEventArgs e)
        {
            ConnectionLost?.Invoke(this, e);
        }

        /// <summary>
        /// Triggers a <see cref="ChannelError"/> event.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        protected virtual void OnError(ChannelErrorEventArgs e)
        {
            ChannelError?.Invoke(this, e);
        }
        

        private void ReceptionLoop()
        {
            try
            {
                // Buffer for reading data
                byte[] bytes = new byte[_options.ReceptionBufferSize];
                string payload = null;
                int byteCount;

                // Get a stream object for reading and writing
                NetworkStream stream = _tcpClient.GetStream();

                while (true)
                {
                    payload = null;
                    if (stream.DataAvailable)
                    {
                        StringBuilder message = new StringBuilder();

                        while (stream.DataAvailable)
                        {
                            // Loop to receive all the data sent by the client.
                            byteCount = stream.Read(bytes, 0, bytes.Length);

                            payload = _options.Encoder.GetString(bytes, 0, byteCount);
                            message.Append(payload);
                        }
                        OnMessageReceived(new MessageReceivedEventArgs(RemoteEndPoint, message.ToString()));
                    }
                    else if (_options.KeepAlive && _tcpClient.Client.Poll(0, SelectMode.SelectRead))
                    {
                        // If the poll returns true, it can be for 3 reasons:
                        // - if Listen() has been called and a connection is pending (we know it is not the case here)
                        // - if data is available for reading (we are going to check now)
                        // - if the connection has been closed, reset, or terminated
                    
                        if (_tcpClient.Client.Available == 0)
                            break;
                    }
                    Thread.Sleep(DEFAULT_DELAY);
                }
                OnConnectionLost(new ConnectionLostEventArgs(RemoteEndPoint, "Socket failed to poll, this suggests connection has been closed, reset or terminated"));
            }
            catch (Exception e)
            {
                OnConnectionLost(new ConnectionLostEventArgs(RemoteEndPoint, e));
            }
        }
    }
}
