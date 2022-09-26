using System;

namespace MemCheck.Application.QueryValidation;

public class NonexistentUserException : InvalidOperationException
{
    public NonexistentUserException(string message) : base(message)
    {
    }
    public NonexistentUserException(string message, Exception innerException) : base(message, innerException)
    {
    }
    public NonexistentUserException()
    {
    }
}
