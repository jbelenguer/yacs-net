using System;

namespace Yacs.Events
{
    /// <summary>
    /// Contains the arguments for when a channel connects to the server.
    /// </summary>
    public class ChannelConnectedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the connected channel.
        /// </summary>
        public IChannel Channel { get; }

        internal ChannelConnectedEventArgs(IChannel channel)
        {
            Channel = channel;
        }

    }
}
