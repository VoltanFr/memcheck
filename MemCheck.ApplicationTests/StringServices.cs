using System;

namespace MemCheck.Application.Tests
{
    public static class StringServices
    {
        public static string RandomString()
        {
            return Guid.NewGuid().ToString();
        }
    }
}