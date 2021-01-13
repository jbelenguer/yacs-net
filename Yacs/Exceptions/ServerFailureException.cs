using System;

namespace Yacs.Exceptions
{
    /// <summary>
    /// Represents an error in the <see cref="Server"/>.
    /// </summary>
    public class ServerFailureException : Exception
    {
        /// <summary>
        /// Creates a new <see cref="ServerFailureException"/>.
        /// </summary>
        /// <param name="message">A message indicating the cause of the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        internal ServerFailureException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
