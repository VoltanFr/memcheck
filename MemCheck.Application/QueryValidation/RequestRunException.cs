using System;

namespace MemCheck.Application.QueryValidation
{
    internal sealed class RequestRunException : Exception
    {
        public RequestRunException(string message) : base(message)
        {
        }
        public RequestRunException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
