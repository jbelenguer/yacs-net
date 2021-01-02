using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Yacs.Events
{
    /// <summary>
    /// Contains the arguments for when a channel throws an error.
    /// </summary>
    public class ChannelErrorEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the errored <see cref="Channel"/> end point.
        /// </summary>
        public ChannelIdentifier EndPoint { get; private set; }
        /// <summary>
        /// Gets a message.
        /// </summary>
        public string AdditionalInfo { get; private set; }
        /// <summary>
        /// Gets the exception that triggered the error.
        /// </summary>
        public Exception Exception { get; private set; }

        internal ChannelErrorEventArgs(ChannelIdentifier remoteEndPoint, Exception ex)
        {
            EndPoint = remoteEndPoint;
            AdditionalInfo = ex.Message;
            Exception = ex;
        }
    }
}
