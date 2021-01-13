using System;

namespace Yacs.Events
{
    /// <summary>
    /// Contains the arguments for when a new byte array message is received.
    /// </summary>
    public class ByteMessageReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the identifier of the channel through which the message was received.
        /// </summary>
        public ChannelIdentifier ChannelIdentifer { get; }

        /// <summary>
        /// Gets the received message.
        /// </summary>
        public byte[] Message { get; }
        
        internal ByteMessageReceivedEventArgs(ChannelIdentifier channelIdentifier, byte[] message)
        {
            ChannelIdentifer = channelIdentifier;
            Message = message;
        }
    }
}
