using System;

namespace Yacs.Events
{
    /// <summary>
    /// Contains the arguments for when a channel disconnects from the server.
    /// </summary>
    public class ChannelDisconnectedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the identifier of the disconnected channel.
        /// </summary>
        public ChannelIdentifier ChannelIdentifier { get; }

        /// <summary>
        /// Gets the exception (if any) that triggered the disconnection.
        /// </summary>
        public Exception Exception { get; }

        internal ChannelDisconnectedEventArgs(ChannelIdentifier channelIdentfier)
        {
            ChannelIdentifier = channelIdentfier;
        }

        internal ChannelDisconnectedEventArgs(ChannelIdentifier channelIdentifier, Exception ex)
        {
            ChannelIdentifier = channelIdentifier;
            Exception = ex;
        }

    }
}
