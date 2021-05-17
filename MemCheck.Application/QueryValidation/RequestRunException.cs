using System;

namespace MemCheck.Application.QueryValidation
{
    public sealed class RequestRunException : Exception
    {
        public RequestRunException(string message) : base(message)
        {
        }
        public RequestRunException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
