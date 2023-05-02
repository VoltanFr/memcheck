using System;

namespace MemCheck.Application.QueryValidation;

// This is an InvalidOperationException since this should never happen: the calling code must check the validity if needed

public class PageSizeTooSmallException : InvalidOperationException
{
    public PageSizeTooSmallException(int receivedValue, int minAcceptedValue, int maxAcceptedValue) : base($"Invalid page index: {receivedValue} (min: {minAcceptedValue}, max: {maxAcceptedValue})")
    {
    }

    public PageSizeTooSmallException()
    {
    }

    public PageSizeTooSmallException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
