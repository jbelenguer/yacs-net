using System;
using System.Collections.Generic;
using Yacs.Events;
using Yacs.Options;

namespace Yacs
{
    /// <summary>
    /// Represents a Yacs server. A server is able to accept many connections from many <see cref="IChannel"/> instances.
    /// </summary>
    public interface IServer
    {
        /// <summary>
        /// Enables or disables new connections. If the <see cref="IServer"/> is discoverable, the flag also affects the discovery feature.
        /// </summary>
        /// <remarks>
        /// NOTE: This means that if you re-enable the <see cref="IServer"/>, discovery will also be re-enabled, no matter what its value was before.
        /// </remarks>
        bool IsEnabled { get; set; }

        /// <summary>
        /// Enables or disables discovery of the <see cref="IServer"/>, if the feature was enabled on creation.
        /// </summary>
        bool IsDiscoveryEnabled { get; set; }

        /// <summary>
        /// Gets the channel identifiers of connected channels.
        /// </summary>
        IReadOnlyList<ChannelIdentifier> Channels { get; }

        /// <summary>
        /// Sends a message to the <see cref="IChannel"/> specified. This method will throw an <see cref="InvalidOperationException"/> if the channel has no <see cref="BaseOptions.Encoder"/> set.
        /// </summary>
        /// <param name="destination">The identifier of the <see cref="IChannel"/> to which the message should be sent.</param>
        /// <param name="message">The string message to send.</param>
        /// <exception cref="InvalidOperationException"></exception>
        void Send(ChannelIdentifier destination, string message);

        /// <summary>
        /// Sends a message to the <see cref="IChannel"/> specified. This method will throw an <see cref="InvalidOperationException"/> if the channel has an <see cref="BaseOptions.Encoder"/> set.
        /// </summary>
        /// <param name="destination">The identifier of the <see cref="IChannel"/> to which the message should be sent.</param>
        /// <param name="message">The byte message to send.</param>
        /// <exception cref="InvalidOperationException"></exception>
        void Send(ChannelIdentifier destination, byte[] message);

        /// <summary>
        /// Gets a value indicating whether a specific <see cref="IChannel"/> is online.
        /// </summary>
        /// <param name="channel">The identifier of the channel.</param>
        /// <returns>True if the channel is online, false otherwise.</returns>
        bool IsChannelOnline(ChannelIdentifier channel);

        /// <summary>
        /// Disconnects a specific <see cref="IChannel"/>.
        /// </summary>
        /// <param name="channel">The identifier of the channel to disconnect.</param>
        void Disconnect(ChannelIdentifier channel);

        /// <summary>
        /// Event triggered when an <see cref="IChannel"/> starts a connection to the <see cref="IServer"/>.
        /// </summary>
        event EventHandler<ConnectionReceivedEventArgs> ConnectionReceived;

        /// <summary>
        /// Event triggered when a discovery request is received from an <see cref="IChannel"/>.
        /// </summary>
        event EventHandler<DiscoveryRequestEventArgs> DiscoveryRequestReceived;

        /// <summary>
        /// Event triggered when a connection to an <see cref="IChannel"/> is lost.
        /// </summary>
        /// <remarks> NOTE: If you are really interested in monitoring channels, you may want to enable
        /// <see cref="ServerOptions.ActiveChannelMonitoring"/>.
        /// </remarks>
        event EventHandler<ConnectionLostEventArgs> ConnectionLost;

        /// <summary>
        /// Event triggered when a string message is received from an <see cref="IChannel"/>.
        /// </summary>
        event EventHandler<StringMessageReceivedEventArgs> StringMessageReceived;

        /// <summary>
        /// Event triggered when a byte array message is received from an <see cref="IChannel"/>.
        /// </summary>
        event EventHandler<ByteMessageReceivedEventArgs> ByteMessageReceived;

        /// <summary>
        /// Event triggered when an <see cref="IChannel"/> throws an error.
        /// </summary>
        event EventHandler<ChannelErrorEventArgs> ChannelError;
    }
}