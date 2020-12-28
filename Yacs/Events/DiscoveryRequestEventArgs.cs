using System;
using System.Net;

namespace Yacs.Events
{
    /// <summary>
    /// Contains the arguments for when a client tries to discover the server.
    /// </summary>
    public class DiscoveryRequestEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the client's end point.
        /// </summary>
        public IPEndPoint EndPoint { get; private set; }

        internal DiscoveryRequestEventArgs(IPEndPoint remoteEndPoint)
        {
            EndPoint = remoteEndPoint;
        }

    }
}
