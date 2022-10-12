using System;

namespace MemCheck.Application.QueryValidation;

// This is an InvalidOperationException since this should never happen: the calling code must trim

public class SearchTextNotTrimmedException : Exception
{
    public SearchTextNotTrimmedException(string message) : base(message)
    {
    }
    public SearchTextNotTrimmedException(string message, Exception innerException) : base(message, innerException)
    {
    }
    public SearchTextNotTrimmedException()
    {
    }
}
