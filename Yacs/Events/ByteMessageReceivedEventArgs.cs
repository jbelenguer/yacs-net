using System;
using System.Collections.Generic;

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
        public IReadOnlyList<byte> Message { get; }
        
        internal ByteMessageReceivedEventArgs(ChannelIdentifier channelIdentifier, IReadOnlyList<byte> message)
        {
            ChannelIdentifer = channelIdentifier;
            Message = message;
        }
    }
}
