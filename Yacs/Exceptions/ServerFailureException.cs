using System;
using System.Collections.Generic;
using System.Text;

namespace Yacs.Exceptions
{
    public class ServerFailureException : ApplicationException
    {
        public ServerFailureException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
