using System;

namespace MemCheck.Domain;

public static class MemCheckUserExtensions
{
    public static string GetUserName(this MemCheckUser user)
    {
        var result = user.UserName;
        return result ?? throw new InvalidProgramException("null user name");
    }
    public static MemCheckEmailAddress GetEmail(this MemCheckUser user)
    {
        if (string.IsNullOrWhiteSpace(user.Email))
            throw new InvalidProgramException("null user email");
        if (string.IsNullOrWhiteSpace(user.UserName))
            throw new InvalidProgramException("null user name");
        return new MemCheckEmailAddress(user.Email, user.UserName);
    }
}
