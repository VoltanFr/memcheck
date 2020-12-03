using System;

namespace MemCheck.Application.QueryValidation
{
    internal sealed class RequestInputException : Exception
    {
        public RequestInputException(string message) : base(message)
        {
        }
        public RequestInputException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
