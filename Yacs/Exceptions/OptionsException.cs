using System;

namespace Yacs.Exceptions
{
    /// <summary>
    /// Represents an error in the configuration options of an <see cref="IServer"/> or <see cref="IChannel"/>.
    /// </summary>
    public class OptionsException : ApplicationException
    {
        /// <summary>
        /// Creates a new <see cref="OptionsException"/>.
        /// </summary>
        /// <param name="message"></param>
        public OptionsException(string message) : base(message)
        {
        }
    }
}
