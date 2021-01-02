using System;
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
        /// Gets the number of online channels connected to the <see cref="IServer"/>.
        /// </summary>
        int ChannelCount { get; }

        /// <summary>
        /// Sends data to a specific <see cref="IChannel"/>.
        /// </summary>
        /// <param name="destination">The identifier of the <see cref="IChannel"/> to which the message should be sent.</param>
        /// <param name="message">The message to send.</param>
        void Send(ChannelIdentifier destination, string message);

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
        /// Event triggered when a message is received from an <see cref="IChannel"/>.
        /// </summary>
        event EventHandler<MessageReceivedEventArgs> MessageReceived;

        /// <summary>
        /// Event triggered when an <see cref="IChannel"/> throws an error.
        /// </summary>
        event EventHandler<ChannelErrorEventArgs> ChannelError;
    }
}