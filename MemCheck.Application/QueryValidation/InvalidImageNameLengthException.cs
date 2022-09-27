using System;

namespace MemCheck.Application.QueryValidation;

public class InvalidImageNameLengthException : RequestInputException
{
    public InvalidImageNameLengthException(string message) : base(message)
    {
    }
    public InvalidImageNameLengthException(string message, Exception innerException) : base(message, innerException)
    {
    }
    public InvalidImageNameLengthException()
    {
    }
}
