using System;

namespace MemCheck.Application.QueryValidation;

// This is an InvalidOperationException since this should never happen: the calling code must check the validity if needed

public class PageSizeTooBigException : InvalidOperationException
{
    public PageSizeTooBigException(int receivedValue, int minAcceptedValue, int maxAcceptedValue) : base($"Invalid page index: {receivedValue} (min: {minAcceptedValue}, max: {maxAcceptedValue})")
    {
    }
    public PageSizeTooBigException(string message, Exception innerException) : base(message, innerException)
    {
    }
    public PageSizeTooBigException()
    {
    }
}
