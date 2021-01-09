using System;
using Yacs.Events;
using Yacs.Options;

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
        /// Sends a message through the channel. This method will throw an <see cref="InvalidOperationException"/> if the channel has no <see cref="BaseOptions.Encoder"/> set.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <exception cref="InvalidOperationException"></exception>
        void Send(string message);

        /// <summary>
        /// Sends a message through the channel. This method will throw an <see cref="InvalidOperationException"/> if the channel has an <see cref="BaseOptions.Encoder"/> set.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <exception cref="InvalidOperationException"></exception>
        void Send(byte[] message);

        /// <summary>
        /// Event triggered to indicate there is a new string message.
        /// </summary>
        event EventHandler<StringMessageReceivedEventArgs> StringMessageReceived;

        /// <summary>
        /// Event triggered to indicate there is a new byte array message.
        /// </summary>
        event EventHandler<ByteMessageReceivedEventArgs> ByteMessageReceived;

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