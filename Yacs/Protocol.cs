using System;
using System.Buffers.Binary;
using Yacs.Events;

namespace Yacs
{
    /// <summary>
    /// Maintains the necessary buffers for applying a length-prefix message framing protocol over a stream.
    /// </summary>
    /// <remarks>
    /// <para>If <see cref="DataReceived"/> raises <see cref="System.Net.ProtocolViolationException"/>, then the stream data should be considered invalid. 
    ///     After that point, no methods should be called on that <see cref="Protocol"/> instance.</para>
    /// <para>Based on the original source: https://blog.stephencleary.com/2009/04/sample-code-length-prefix-message.html. </para>
    /// </remarks>
    internal class Protocol
    {
        private const int HEADER_SIZE = 4;

        private readonly int _maxMessageSize;

        private readonly byte[] _headerBuffer;
        private byte[] _payloadBuffer;

        private int _sectionByteCount;

        /// <summary>
        /// Creates a new <see cref="Protocol"/> instance. We should create a protocol object per stream.
        /// </summary>
        /// <param name="maxMessageSize">Protection against DoS</param>
        internal Protocol(int maxMessageSize)
        {
            _headerBuffer = new byte[HEADER_SIZE];
            _maxMessageSize = maxMessageSize;
        }

        /// <summary>
        /// Creates a new data message, wrapping the given payload with a header.
        /// </summary>
        /// <param name="messagePayload">Message to send.</param>
        /// <returns></returns>
        internal static byte[] CreateDataMessage(byte[] messagePayload)
        {
            if (messagePayload?.Length <= 0)
                throw new System.Net.ProtocolViolationException("The message is empty");
            var messageHeader = GenerateHeader(messagePayload.Length);

            byte[] fullMessage = new byte[HEADER_SIZE + messagePayload.Length];
            messageHeader.CopyTo(fullMessage, 0);
            messagePayload.CopyTo(fullMessage, HEADER_SIZE);

            return fullMessage;
        }

        /// <summary>
        /// Creates a new discovery message which is essentially an empty message with a header.
        /// </summary>
        /// <returns></returns>
        internal static byte[] CreateDiscoveryRequestMessage()
        {
            return GenerateHeader(0);
        }

        /// <summary>
        /// Creates a new discovery response message with the given port number.
        /// </summary>
        /// <param name="portNumber">TCP port number to be used.</param>
        /// <returns></returns>
        internal static byte[] CreateDiscoveryResponseMessage(int portNumber)
        {
            var messagePayload = new byte[sizeof(int)];
            BinaryPrimitives.WriteInt32BigEndian(messagePayload, portNumber);

            byte[] fullMessage = CreateDataMessage(messagePayload);

            return fullMessage;
        }

        /// <summary>
        /// Parses a discovery reply, returning the port in which the remote <see cref="Hub"/> is listening. 
        /// </summary>
        /// <param name="message">Message to parse.</param>
        /// <returns></returns>
        internal static int DiscoveryResponseReceived(byte[] message)
        {
            var messageSize = ReadHeader(message);
            int portRead = 0;
            if (messageSize == sizeof(int))
            {
                byte[] messagePayload = new byte[sizeof(int)];
                Array.Copy(message, HEADER_SIZE, messagePayload, 0, messagePayload.Length);
                portRead = BinaryPrimitives.ReadInt32BigEndian(messagePayload);
            }
            return portRead;
        }

        /// <summary>
        /// Validates a message for a discovery request. Returns true if the request is valid.
        /// </summary>
        /// <param name="discoveryRequest"></param>
        /// <returns></returns>
        internal static bool ValidateDiscoveryRequest(byte[] discoveryRequest)
        {
            var payloadSize = ReadHeader(discoveryRequest);
            return discoveryRequest.Length == HEADER_SIZE + payloadSize;
        }

        /// <summary>
        /// Processes the content of a buffer, to be able to extract messages from the byte array.
        /// </summary>
        /// <param name="incomingBuffer">Byte buffer to read bytes from.</param>
        /// <param name="bytesAvailable">Number of bytes available in the buffer.</param>
        /// <exception cref="System.Net.ProtocolViolationException"></exception>
        internal void DataReceived(byte[] incomingBuffer, int bytesAvailable)
        {
            if (incomingBuffer.Length < bytesAvailable)
            {
                throw new Exception("");
            }
            int index = 0;
            while (index != bytesAvailable)
            {
                // Determine what section we are reading; header or payload.
                int transferredBytes;
                if (_payloadBuffer == null)
                {
                    transferredBytes = TransferBytesToSectionBuffer(incomingBuffer, bytesAvailable, index, _headerBuffer);
                }
                else
                {
                    transferredBytes = TransferBytesToSectionBuffer(incomingBuffer, bytesAvailable, index, _payloadBuffer);
                }
                index += transferredBytes;
                _sectionByteCount += transferredBytes;
                ParseSectionBuffers();
            }
        }

        internal event EventHandler<ProtocolMessageReceivedEventArgs> ProtocolMessageReceived;

        /// <summary>
        /// Triggers a <see cref="ProtocolMessageReceived"/> event.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        protected virtual void OnProtocolMessageReceived(ProtocolMessageReceivedEventArgs e)
        {
            ProtocolMessageReceived?.Invoke(this, e);
        }

        private int TransferBytesToSectionBuffer(byte[] incoming, int incomingByteCount, int startIndex, byte[] sectionBuffer)
        {
            var bytesAvailable = incomingByteCount - startIndex;
            var sectionBytesMissing = sectionBuffer.Length - _sectionByteCount;
            int bytesToTransfer = Math.Min(sectionBytesMissing, bytesAvailable);

            Array.Copy(incoming, startIndex, sectionBuffer, _sectionByteCount, bytesToTransfer);

            return bytesToTransfer;
        }

        /// <summary>
        /// Checks the status of the section buffers to see if we have a message already. The two conditions that can trigger an action in
        /// this method are:
        /// <para>    - We have no payload buffer yet, and we have received enough bytes to read the header.</para>
        /// <para>    - We have a payload buffer and we have received the expected number of bytes.</para>
        /// </summary>
        /// <exception cref="System.Net.ProtocolViolationException">Indicates an unrecoverable problem. The protocol instance must be discarded.</exception>
        private void ParseSectionBuffers()
        {
            if (_payloadBuffer == null)
            {
                if (_sectionByteCount == HEADER_SIZE)
                {
                    int payloadLength = ReadHeader(_headerBuffer);

                    if (payloadLength < 0)
                        throw new System.Net.ProtocolViolationException("Message header seems corrupt and this may indicate a desynchronisation.");

                    if (_maxMessageSize > 0 && payloadLength > _maxMessageSize)
                        throw new System.Net.ProtocolViolationException($"Message length {payloadLength.ToString(System.Globalization.CultureInfo.InvariantCulture)} is larger than maximum message size {_maxMessageSize.ToString(System.Globalization.CultureInfo.InvariantCulture)}");

                    if (payloadLength == 0)
                    {
                        _payloadBuffer = null;
                    }
                    else
                    {
                        _payloadBuffer = new byte[payloadLength];
                    }
                    _sectionByteCount = 0;
                }
            }
            else
            {
                if (_sectionByteCount == _payloadBuffer.Length)
                {
                    var args = new ProtocolMessageReceivedEventArgs(_payloadBuffer);
                    OnProtocolMessageReceived(args);

                    // Start reading the length buffer again
                    _payloadBuffer = null;
                    _sectionByteCount = 0;
                }
            }
        }

        private static byte[] GenerateHeader(int messageSize)
        {
            var messageHeader = new byte[HEADER_SIZE];
            BinaryPrimitives.WriteInt32BigEndian(messageHeader, messageSize);
            return messageHeader;
        }

        private static int ReadHeader(byte[] buffer)
        {
            return BinaryPrimitives.ReadInt32BigEndian(buffer);
        }
    }
}
