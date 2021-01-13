using System;

namespace Yacs.Events
{
    /// <summary>
    /// Contains the arguments for when a channel is refused a connection by the server.
    /// </summary>
    public class ChannelRefusedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the identifier of the refused channel.
        /// </summary>
        public ChannelIdentifier ChannelIdentifier { get; }

        internal ChannelRefusedEventArgs(ChannelIdentifier channelIdentifier)
        {
            ChannelIdentifier = channelIdentifier;
        }
    }
}
