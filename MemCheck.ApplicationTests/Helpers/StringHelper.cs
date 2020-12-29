using System;

namespace MemCheck.Application.Tests.Helpers
{
    internal static class StringHelper
    {
        public static string RandomString()
        {
            return Guid.NewGuid().ToString();
        }
    }
}