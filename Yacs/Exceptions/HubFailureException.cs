using System;

namespace Yacs.Exceptions
{
    /// <summary>
    /// Represents an error in the <see cref="Hub"/>.
    /// </summary>
    public class HubFailureException : Exception
    {
        /// <summary>
        /// Creates a new <see cref="HubFailureException"/>.
        /// </summary>
        /// <param name="message">A message indicating the cause of the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        internal HubFailureException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
