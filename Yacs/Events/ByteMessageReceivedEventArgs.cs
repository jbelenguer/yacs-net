using System;

namespace Yacs.Events
{
    /// <summary>
    /// Contains arguments for when a new byte array message is received.
    /// </summary>
    public class ByteMessageReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the client's end point.
        /// </summary>
        public ChannelIdentifier EndPoint { get; private set; }
        /// <summary>
        /// Gets the message received.
        /// </summary>
        public byte[] Message { get; private set; }
        
        internal ByteMessageReceivedEventArgs(ChannelIdentifier remoteEndPoint, byte[] message)
        {
            EndPoint = remoteEndPoint;
            Message = message;
        }

    }
}
