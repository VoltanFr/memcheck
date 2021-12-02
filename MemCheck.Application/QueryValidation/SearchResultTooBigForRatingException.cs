using System;

namespace MemCheck.Application.QueryValidation
{
    internal sealed class SearchResultTooBigForRatingException : Exception
    {
        public SearchResultTooBigForRatingException() : base()
        {
        }
    }
}
