using System;

namespace Yacs.Exceptions
{
    /// <summary>
    /// Represents an error in the configuration options of an <see cref="IServer"/> or <see cref="IChannel"/>.
    /// </summary>
    public class OptionsException : Exception
    {
        /// <summary>
        /// Creates a new <see cref="OptionsException"/>.
        /// </summary>
        /// <param name="message">A message indicating the cause of the exception.</param>
        internal OptionsException(string message) : base(message)
        {
        }
    }
}
