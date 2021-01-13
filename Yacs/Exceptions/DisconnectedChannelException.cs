using System;

namespace Yacs.Exceptions
{
    /// <summary>
    /// Represents an error due to an <see cref="IChannel"/> being disconnected.
    /// </summary>
    public class DisconnectedChannelException : Exception
    {
        /// <summary>
        /// Gets the identifier of the channel that is disconnected.
        /// </summary>
        public ChannelIdentifier ChannelIdentifier { get; }

        /// <summary>
        /// Creates a new <see cref="DisconnectedChannelException"/>.
        /// </summary>
        /// <param name="channelIdentifier">The identifier of the channel that is disconnected.</param>
        internal DisconnectedChannelException(ChannelIdentifier channelIdentifier) : base($"The channel {channelIdentifier} is not connected.")
        {
            ChannelIdentifier = channelIdentifier;
        }
    }
}