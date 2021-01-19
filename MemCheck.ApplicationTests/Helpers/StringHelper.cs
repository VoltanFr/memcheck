using System;
using System.Text;

namespace MemCheck.Application.Tests.Helpers
{
    internal static class StringHelper
    {
        public static string RandomString(int? length = null)
        {
            var result = new StringBuilder();
            do
            {
                result.Append(Guid.NewGuid().ToString());
            }
            while (length != null && result.Length < length);
            if (length != null)
                result.Length = length.Value;
            return result.ToString();
        }
    }
}