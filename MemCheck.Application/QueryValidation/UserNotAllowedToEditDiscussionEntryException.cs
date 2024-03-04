using System;

namespace MemCheck.Application.QueryValidation;

public class UserNotAllowedToEditDiscussionEntryException : InvalidOperationException
{
    public UserNotAllowedToEditDiscussionEntryException(string message) : base(message)
    {
    }
    public UserNotAllowedToEditDiscussionEntryException(string message, Exception innerException) : base(message, innerException)
    {
    }
    public UserNotAllowedToEditDiscussionEntryException()
    {
    }
}
