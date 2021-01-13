using System;

namespace Yacs.Events
{
    /// <summary>
    /// Contains the arguments for when a new string message is received.
    /// </summary>
    public class StringMessageReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the identifier of the channel through which the message was received.
        /// </summary>
        public ChannelIdentifier ChannelIdentifier { get; }

        /// <summary>
        /// Gets the received message.
        /// </summary>
        public string Message { get; }

        internal StringMessageReceivedEventArgs(ChannelIdentifier channelIdentifier, string message)
        {
            ChannelIdentifier = channelIdentifier;
            Message = message;
        }
    }
}
