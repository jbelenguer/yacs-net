using System;

namespace Yacs.Exceptions
{
    /// <summary>
    /// Represents an error while trying to send a message.
    /// </summary>
    public class SendMessageException : Exception
    {
        /// <summary>
        /// Gets the identifier of the channel through which the message failed to send.
        /// </summary>
        public ChannelIdentifier ChannelIdentifier { get; }

        /// <summary>
        /// Creates a new <see cref="SendMessageException"/>.
        /// </summary>
        /// <param name="channelIdentifier">The identifier of the channel through which the message failed to send.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        internal SendMessageException(ChannelIdentifier channelIdentifier, Exception innerException) : base("Error sending message.", innerException)
        {
            ChannelIdentifier = channelIdentifier;
        }
    }
}