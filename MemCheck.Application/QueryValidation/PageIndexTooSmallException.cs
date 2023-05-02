using System;

namespace MemCheck.Application.QueryValidation;

// This is an InvalidOperationException since this should never happen: the calling code must check the validity if needed

public class PageIndexTooSmallException : InvalidOperationException
{
    public PageIndexTooSmallException(int receivedValue) : base($"First page is numbered 1, received a request for page {receivedValue}")
    {
    }
    public PageIndexTooSmallException(string message, Exception innerException) : base(message, innerException)
    {
    }
    public PageIndexTooSmallException()
    {
    }
}
