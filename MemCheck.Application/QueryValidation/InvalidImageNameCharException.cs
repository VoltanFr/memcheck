using System;

namespace MemCheck.Application.QueryValidation;

public class InvalidImageNameCharException : RequestInputException
{
    public InvalidImageNameCharException(string message) : base(message)
    {
    }
    public InvalidImageNameCharException(string message, Exception innerException) : base(message, innerException)
    {
    }
    public InvalidImageNameCharException()
    {
    }
}
