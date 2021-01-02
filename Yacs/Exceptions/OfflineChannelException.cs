﻿using System;
using System.Net;
using System.Runtime.Serialization;

namespace Yacs.Exceptions
{
    /// <summary>
    /// Represents an error due to a <see cref="Channel"/> being offline.
    /// </summary>
    [Serializable]
    public class OfflineChannelException : Exception
    {
        /// <summary>
        /// Gets the <see cref="EndPoint"/> that seems offline.
        /// </summary>
        public string EndPoint { get; private set; }

        /// <summary>
        /// Creates a new <see cref="OfflineChannelException"/>.
        /// </summary>
        /// <param name="remoteEndpoint"></param>
        public OfflineChannelException(string remoteEndpoint) : base($"The endpoint {remoteEndpoint} is not registered as an online client.")
        {
            EndPoint = remoteEndpoint;
        }
    }
}