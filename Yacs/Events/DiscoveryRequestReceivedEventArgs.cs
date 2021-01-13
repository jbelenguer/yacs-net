using System;

namespace Yacs.Events
{
    /// <summary>
    /// Contains the arguments for when a client tries to discover the server.
    /// </summary>
    public class DiscoveryRequestReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the client's end point.
        /// </summary>
        public string EndPoint { get; }

        internal DiscoveryRequestReceivedEventArgs(string remoteEndPoint)
        {
            EndPoint = remoteEndPoint;
        }
    }
}
