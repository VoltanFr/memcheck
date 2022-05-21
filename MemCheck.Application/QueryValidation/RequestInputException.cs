using System;

namespace MemCheck.Application.QueryValidation;

public class RequestInputException : Exception
{
    public RequestInputException(string message) : base(message)
    {
    }
    public RequestInputException(string message, Exception innerException) : base(message, innerException)
    {
    }
    public RequestInputException()
    {
    }
}
