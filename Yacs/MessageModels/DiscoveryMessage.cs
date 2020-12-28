using System;
using System.Collections.Generic;
using System.Text;

namespace Yacs.MessageModels
{
    /// <summary>
    /// Represents a utility to work with discovery messages.
    /// </summary>
    internal static class DiscoveryMessage 
    {
        /// <summary>
        /// Returns a discoverty request message.
        /// </summary>
        internal static byte[] Request => new byte[] { 1, 5, 4 };

        /// <summary>
        /// Returns a discovery reply message, encoding the server port number.
        /// </summary>
        /// <param name="portNumber">Port number in which server is listening.</param>
        /// <returns></returns>
        internal static byte[] CreateReply(int portNumber)
        {
            byte[] intBytes = BitConverter.GetBytes(portNumber);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(intBytes);
            
            return intBytes;
        }

        /// <summary>
        /// Validates if a message is a valid discovery request.
        /// </summary>
        /// <param name="message">Message to parse.</param>
        /// <returns></returns>
        internal static bool IsValidRequest(byte[] message)
        {
            var pattern = Request;
            if (message.Length == pattern.Length)
            {
                for (int i = 0; i < pattern.Length; i++)
                {
                    if (message[i] != pattern[i])
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Tries to parse a discovery reply from a server, returning if the attempt was successful. In which case the port is returned in the output argument.
        /// </summary>
        /// <param name="message">Message to try parse.</param>
        /// <param name="port"></param>
        /// <returns></returns>
        internal static bool TryParse(byte[] message, out int port)
        {
            try
            {
                port = ParseDiscoveryPort(message);
                return true;
            }
            catch(Exception)
            {
                port = 0;
                return false;
            }
        }

        /// <summary>
        /// Parses a discovery reply from a server, returning the port in which the server is listening. 
        /// </summary>
        /// <param name="message">Message to parse.</param>
        /// <returns></returns>
        internal static int ParseDiscoveryPort(byte[] message)
        {
            if (BitConverter.IsLittleEndian)
                Array.Reverse(message);
            return BitConverter.ToInt32(message, 0);
        }
    }
}
