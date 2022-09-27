using System;

namespace MemCheck.Application.QueryValidation;

public class ImageNameNotTrimmedException : Exception
{
    public ImageNameNotTrimmedException(string message) : base(message)
    {
    }
    public ImageNameNotTrimmedException(string message, Exception innerException) : base(message, innerException)
    {
    }
    public ImageNameNotTrimmedException()
    {
    }
}
