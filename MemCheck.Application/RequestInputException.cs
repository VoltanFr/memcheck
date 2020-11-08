using System;

namespace MemCheck.Application
{
    public sealed class RequestInputException : Exception
    {
        public RequestInputException(string message) : base(message)
        {
        }
        public RequestInputException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
    public sealed class SearchResultTooBigForRatingException : Exception
    {
        public SearchResultTooBigForRatingException() : base()
        {
        }
    }
}
