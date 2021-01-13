using System;

namespace Yacs.Events
{
    /// <summary>
    /// Contains the arguments for when a channel connects to the server.
    /// </summary>
    public class ChannelConnectedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the identifier of the connected channel.
        /// </summary>
        public ChannelIdentifier ChannelIdentifier { get; }

        internal ChannelConnectedEventArgs(ChannelIdentifier channelIdentifier)
        {
            ChannelIdentifier = channelIdentifier;
        }

    }
}
