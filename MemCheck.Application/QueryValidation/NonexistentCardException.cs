using System;

namespace MemCheck.Application.QueryValidation;

public class NonexistentCardException : InvalidOperationException
{
    public NonexistentCardException(string message) : base(message)
    {
    }
    public NonexistentCardException(string message, Exception innerException) : base(message, innerException)
    {
    }
    public NonexistentCardException()
    {
    }
}
