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
        public EndPoint EndPoint { get; private set; }
        /// <summary>
        /// Gets a message.
        /// </summary>
        public string AdditionalInfo { get; private set; }
        /// <summary>
        /// Gets the exception (if any) that triggered the disconnection.
        /// </summary>
        public Exception Exception { get; private set; }
        
        internal ConnectionLostEventArgs(EndPoint remoteEndPoint, string additionalInfo = null)
        {
            EndPoint = remoteEndPoint;
            AdditionalInfo = additionalInfo;
        }

        internal ConnectionLostEventArgs(EndPoint remoteEndPoint, Exception ex)
        {
            EndPoint = remoteEndPoint;
            AdditionalInfo = ex.Message;
            Exception = ex;
        }

    }
}
