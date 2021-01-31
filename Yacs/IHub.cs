using System;
using Yacs.Events;
using Yacs.Options;

namespace Yacs
{
    /// <summary>
    /// Represents a Yacs server. A server is able to accept many connections from many <see cref="IChannel"/> instances.
    /// </summary>
    public interface IHub
    {
        /// <summary>
        /// Enables or disables new connections. If the <see cref="IHub"/> is discoverable, the flag also affects the discovery feature.
        /// </summary>
        /// <remarks>
        /// NOTE: This means that if you re-enable the <see cref="IHub"/>, discovery will also be re-enabled, no matter what its value was before.
        /// </remarks>
        bool IsEnabled { get; set; }

        /// <summary>
        /// Enables or disables discovery of the <see cref="IHub"/>, if the feature was enabled on creation.
        /// </summary>
        bool IsDiscoveryEnabled { get; set; }

        /// <summary>
        /// Event triggered when a discovery request is received from an <see cref="IChannel"/>.
        /// </summary>
        event EventHandler<DiscoveryRequestReceivedEventArgs> DiscoveryRequestReceived;

        /// <summary>
        /// Event triggered when an <see cref="IChannel"/> connects to the <see cref="IHub"/>.
        /// </summary>
        event EventHandler<ChannelConnectedEventArgs> ChannelConnected;

        /// <summary>
        /// Event triggered when a connection to an <see cref="IChannel"/> is lost.
        /// </summary>
        /// <remarks> NOTE: If you are really interested in monitoring channels, you may want to enable
        /// <see cref="HubOptions.ActiveChannelMonitoring"/>.
        /// </remarks>
        event EventHandler<ChannelDisconnectedEventArgs> ChannelDisconnected;

        /// <summary>
        /// Event triggered when a connection to an <see cref="IChannel"/> is refused.
        /// </summary>
        event EventHandler<ChannelRefusedEventArgs> ChannelRefused;
    }
}