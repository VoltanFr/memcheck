using System;

namespace MemCheck.Application.QueryValidation;

public sealed class SearchResultTooBigForRatingException : Exception
{
    public SearchResultTooBigForRatingException() : base()
    {
    }
    public SearchResultTooBigForRatingException(string message) : base(message)
    {
    }
    public SearchResultTooBigForRatingException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
