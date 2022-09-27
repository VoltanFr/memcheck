using System;

namespace MemCheck.Application.QueryValidation;

public sealed class ImageUsedInCardException : Exception
{
    public ImageUsedInCardException(string message) : base(message)
    {
    }
    public ImageUsedInCardException(string message, Exception innerException) : base(message, innerException)
    {
    }
    public ImageUsedInCardException()
    {
    }
}
