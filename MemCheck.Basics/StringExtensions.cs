using System;
using System.Collections.Generic;

namespace MemCheck.Basics;

public static class StringExtensions
{
    public static string Truncate(this string str, int maxLength, bool addElipsis = true)
    {
        if (string.IsNullOrEmpty(str))
            return "";

        if (str.Length <= maxLength)
            return str;

        var truncated = str.AsSpan(0, maxLength);

        if (!addElipsis)
            return truncated.ToString();

        return string.Concat(truncated, "[...]");
    }
    public static KeyValuePair<string, TValue> PairedWith<TValue>(this string str, TValue value)
    {
        return new KeyValuePair<string, TValue>(str, value);
    }
}
