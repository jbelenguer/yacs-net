using System;

namespace Yacs.Events
{
    internal class ProtocolMessageReceivedEventArgs : EventArgs
    {
        internal byte[] Message { get; }

        internal ProtocolMessageReceivedEventArgs(byte[] message)
        {
            Message = message;
        }
    }
}
