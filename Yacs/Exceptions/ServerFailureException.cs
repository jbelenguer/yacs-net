using System;

namespace Yacs.Exceptions
{
    /// <summary>
    /// Represents an error in the <see cref="Server"/>.
    /// </summary>
    public class ServerFailureException : ApplicationException
    {
        /// <summary>
        /// Creates a new <see cref="ServerFailureException"/>.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public ServerFailureException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
