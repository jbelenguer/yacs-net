using System;
using System.Net;
using System.Runtime.Serialization;

namespace Yacs.Exceptions
{
    [Serializable]
    public class OfflineChannelException : Exception
    {
        public EndPoint EndPoint { get; private set; }

        public OfflineChannelException(EndPoint remoteEndpoint) : base($"The endpoint {remoteEndpoint} is not registered as an online client.")
        {
            EndPoint = remoteEndpoint;
        }
    }
}