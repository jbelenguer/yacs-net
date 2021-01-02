using System;
using System.Net;

namespace Yacs.Events
{
    /// <summary>
    /// Contains the arguments for when a client disconnects from the server.
    /// </summary>
    public class ConnectionLostEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the disconnected client's end point.
        /// </summary>
        public ChannelIdentifier EndPoint { get; private set; }
        /// <summary>
        /// Gets a message.
        /// </summary>
        public string AdditionalInfo { get; private set; }
        /// <summary>
        /// Gets the exception (if any) that triggered the disconnection.
        /// </summary>
        public Exception Exception { get; private set; }
        
        internal ConnectionLostEventArgs(ChannelIdentifier remoteEndPoint, string additionalInfo = null)
        {
            EndPoint = remoteEndPoint;
            AdditionalInfo = additionalInfo;
        }

        internal ConnectionLostEventArgs(ChannelIdentifier remoteEndPoint, Exception ex)
        {
            EndPoint = remoteEndPoint;
            AdditionalInfo = ex.Message;
            Exception = ex;
        }

    }
}
