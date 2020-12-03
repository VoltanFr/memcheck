using System;

namespace MemCheck.Application.QueryValidation
{
    public sealed class SearchResultTooBigForRatingException : Exception
    {
        public SearchResultTooBigForRatingException() : base()
        {
        }
    }
}
