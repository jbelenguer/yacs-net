using System;
using System.Net;

namespace Yacs.Events
{
    /// <summary>
    /// Contains the arguments for when a client connects to the server.
    /// </summary>
    public class ConnectionReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the new client's end point.
        /// </summary>
        public string EndPoint { get; private set; }

        internal ConnectionReceivedEventArgs(string remoteEndPoint)
        {
            EndPoint = remoteEndPoint;
        }

    }
}
