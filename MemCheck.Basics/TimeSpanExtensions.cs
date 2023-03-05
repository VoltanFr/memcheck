using System;
using System.Globalization;

namespace MemCheck.Basics;

public static class TimeSpanExtensions
{
    public static string ToStringWithoutMs(this TimeSpan ts)
    {
        return ts.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture);
    }
}
