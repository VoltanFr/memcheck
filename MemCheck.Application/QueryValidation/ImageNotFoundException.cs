using System;

namespace MemCheck.Application.QueryValidation;

public sealed class ImageNotFoundException : Exception
{
    public ImageNotFoundException(string message) : base(message)
    {
    }
    public ImageNotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }
    public ImageNotFoundException()
    {
    }
}
