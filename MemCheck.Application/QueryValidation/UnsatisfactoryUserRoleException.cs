using System;

namespace MemCheck.Application.QueryValidation;

public class UnsatisfactoryUserRoleException : InvalidOperationException
{
    public UnsatisfactoryUserRoleException(string message) : base(message)
    {
    }
    public UnsatisfactoryUserRoleException(string message, Exception innerException) : base(message, innerException)
    {
    }
    public UnsatisfactoryUserRoleException()
    {
    }
}
