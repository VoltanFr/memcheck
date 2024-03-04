using System;

namespace MemCheck.Application.QueryValidation;

public class NonexistentCardDiscussionEntryException : InvalidOperationException
{
    public NonexistentCardDiscussionEntryException(string message) : base(message)
    {
    }
    public NonexistentCardDiscussionEntryException(string message, Exception innerException) : base(message, innerException)
    {
    }
    public NonexistentCardDiscussionEntryException()
    {
    }
}
