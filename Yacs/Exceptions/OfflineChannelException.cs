using System;

namespace Yacs.Exceptions
{
    /// <summary>
    /// Represents an error due to an <see cref="IChannel"/> being offline.
    /// </summary>
    public class OfflineChannelException : Exception
    {
        /// <summary>
        /// Gets the <see cref="EndPoint"/> that seems offline.
        /// </summary>
        public ChannelIdentifier EndPoint { get; private set; }

        /// <summary>
        /// Creates a new <see cref="OfflineChannelException"/>.
        /// </summary>
        /// <param name="remoteEndpoint"></param>
        public OfflineChannelException(ChannelIdentifier remoteEndpoint) : base($"The endpoint {remoteEndpoint} is not registered as an online client.")
        {
            EndPoint = remoteEndpoint;
        }
    }
}