using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Yacs.Events
{
    /// <summary>
    /// Contains arguments for when a new message is received.
    /// </summary>
    public class MessageReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the client's end point.
        /// </summary>
        public string EndPoint { get; private set; }
        /// <summary>
        /// Gets the message received.
        /// </summary>
        public string Message { get; private set; }
        
        internal MessageReceivedEventArgs(string remoteEndPoint, string message)
        {
            EndPoint = remoteEndPoint;
            Message = message;
        }

    }
}
