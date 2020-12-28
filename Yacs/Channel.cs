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

        private readonly int _messageBufferSize;
        private readonly Encoding _encoder;
        private readonly TcpClient _tcpClient;
        private readonly CancellationTokenSource _source = new CancellationTokenSource();
        private readonly Task _messageReceptionTask;
        private bool disposedValue;

        public bool Online { get; private set; }

        public EndPoint RemoteEndPoint { get; private set; }

        internal Channel(TcpClient tcpClient, ChannelOptions options)
        {
            RemoteEndPoint = tcpClient.Client.RemoteEndPoint;
            _messageBufferSize = options.ReceptionBufferSize;
            _encoder = options.Encoder;
            _tcpClient = tcpClient;
            _messageReceptionTask = Task.Run(ReceptionLoop, _source.Token);
        }

        /// <summary>
        /// Creates a new communication channel for a client. 
        /// </summary>
        /// <param name="serverUrl">Url to connect to.</param>
        /// <param name="port">TCP/UDP port number.</param>
        public Channel(string serverUrl, int port, ChannelOptions options = null) 
        {
            if (options == null)
            {
                options = new ChannelOptions();
            }

            _messageBufferSize = options.ReceptionBufferSize;
            _encoder = options.Encoder;
            _tcpClient = new TcpClient(serverUrl, port);
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
                var msg = _encoder.GetBytes(message);

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

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Event triggered when a new message is received in the <see cref="Channel"/>.
        /// </summary>
        public event EventHandler<MessageReceivedEventArgs> MessageReceived;

        /// <summary>
        /// Event triggered when the <see cref="Channel"/>'s connection is lost.
        /// </summary>
        public event EventHandler<ConnectionLostEventArgs> ConnectionLost;

        /// <summary>
        /// Event triggered when there is an error.
        /// </summary>
        public event EventHandler<ChannelErrorEventArgs> ChannelError;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _source?.Cancel();
                    _tcpClient?.Close();
                    _source?.Dispose();
                    _messageReceptionTask.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        protected virtual void OnMessageReceived(MessageReceivedEventArgs e)
        {
            MessageReceived?.Invoke(this, e);
        }

        protected virtual void OnConnectionLost(ConnectionLostEventArgs e)
        {
            ConnectionLost?.Invoke(this, e);
        }

        protected virtual void OnError(ChannelErrorEventArgs e)
        {
            ChannelError?.Invoke(this, e);
        }
        

        private void ReceptionLoop()
        {
            try
            {
                // Buffer for reading data
                byte[] bytes = new byte[_messageBufferSize];
                string payload = null;
                int byteCount;

                // Get a stream object for reading and writing
                NetworkStream stream = _tcpClient.GetStream();

                while (_tcpClient.Client.Poll(10000, SelectMode.SelectWrite))
                {
                    payload = null;
                    if (stream.DataAvailable)
                    {
                        StringBuilder message = new StringBuilder();

                        while (stream.DataAvailable)
                        {
                            // Loop to receive all the data sent by the client.
                            byteCount = stream.Read(bytes, 0, bytes.Length);

                            payload = _encoder.GetString(bytes, 0, byteCount);
                            message.Append(payload);
                        }
                        OnMessageReceived(new MessageReceivedEventArgs(RemoteEndPoint, message.ToString()));
                    }
                    Thread.Sleep(DEFAULT_DELAY);
                }
                OnConnectionLost(new ConnectionLostEventArgs(RemoteEndPoint, $"Socket poll failed to {SelectMode.SelectRead:G}"));
            }
            catch (Exception e)
            {
                OnConnectionLost(new ConnectionLostEventArgs(RemoteEndPoint, e));
            }
        }
    }
}
