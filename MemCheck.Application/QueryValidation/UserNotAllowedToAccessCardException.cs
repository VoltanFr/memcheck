using System;

namespace MemCheck.Application.QueryValidation;

public class UserNotAllowedToAccessCardException : InvalidOperationException
{
    public UserNotAllowedToAccessCardException(string message) : base(message)
    {
    }
    public UserNotAllowedToAccessCardException(string message, Exception innerException) : base(message, innerException)
    {
    }
    public UserNotAllowedToAccessCardException()
    {
    }
}
