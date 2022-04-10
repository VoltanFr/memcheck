using System;

namespace MemCheck.Basics
{
    public static class StringExtensions
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Conditional is more complicated")]
        public static string Truncate(this string str, int maxLength)
        {
            if (string.IsNullOrEmpty(str))
                return "";

            if (str.Length <= maxLength)
                return str;

            return string.Concat(str.AsSpan(0, maxLength), "[...]");
        }
    }
}
