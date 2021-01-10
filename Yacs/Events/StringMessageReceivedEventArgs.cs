using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Yacs.Events
{
    /// <summary>
    /// Contains arguments for when a new string message is received.
    /// </summary>
    public class StringMessageReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the client's end point.
        /// </summary>
        public ChannelIdentifier EndPoint { get; private set; }
        /// <summary>
        /// Gets the message received.
        /// </summary>
        public string Message { get; private set; }
        
        internal StringMessageReceivedEventArgs(ChannelIdentifier remoteEndPoint, string message)
        {
            EndPoint = remoteEndPoint;
            Message = message;
        }

    }
}
