using System;

namespace Yacs.Exceptions
{
    /// <summary>
    /// Represents an error due to an <see cref="IChannel"/> being offline.
    /// </summary>
    public class OfflineChannelException : Exception
    {
        /// <summary>
        /// Gets the <see cref="Channel"/> that seems offline.
        /// </summary>
        public ChannelIdentifier Channel { get; private set; }

        /// <summary>
        /// Creates a new <see cref="OfflineChannelException"/>.
        /// </summary>
        /// <param name="channelIdentifier"></param>
        public OfflineChannelException(ChannelIdentifier channelIdentifier) : base($"The endpoint {channelIdentifier} is not registered as an online client.")
        {
            Channel = channelIdentifier;
        }
    }
}