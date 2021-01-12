using System;

namespace Yacs.Exceptions
{
    /// <summary>
    /// Represents an error while trying to send a message.
    /// </summary>
    public class SendMessageException : Exception
    {
        /// <summary>
        /// Gets the <see cref="ChannelIdentifier"/> that failed sending.
        /// </summary>
        public ChannelIdentifier Channel { get; private set; }

        /// <summary>
        /// Creates a new <see cref="SendMessageException"/>.
        /// </summary>
        /// <param name="channelIdentifier"></param>
        /// <param name="exception"></param>
        public SendMessageException(ChannelIdentifier channelIdentifier, Exception exception) : base("Error sending message", exception)
        {
            Channel = channelIdentifier;
        }
    }
}