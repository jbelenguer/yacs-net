using System;
using Yacs.Events;

namespace Yacs
{
    /// <summary>
    /// Represents a communication channel.
    /// </summary>
    public interface IChannel
    {
        /// <summary>
        /// Gets the identifier for this channel.
        /// </summary>
        ChannelIdentifier Identifier { get; }

        /// <summary>
        /// Sends a message through the channel.
        /// </summary>
        /// <param name="message">The message to send.</param>
        void Send(string message);

        /// <summary>
        /// Event triggered to indicate there is a new message.
        /// </summary>
        event EventHandler<MessageReceivedEventArgs> MessageReceived;

        /// <summary>
        /// Event triggered to indicate the <see cref="IChannel"/> connection was lost.
        /// </summary>
        event EventHandler<ConnectionLostEventArgs> ConnectionLost;

        /// <summary>
        /// Event triggered to indicate the <see cref="IChannel"/> connection experienced an error.
        /// </summary>
        event EventHandler<ChannelErrorEventArgs> ChannelError;
    }
}