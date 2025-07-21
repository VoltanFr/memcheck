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
        return new MemCheckEmailAddress(user.Email ?? "", user.UserName ?? "");
    }
}
