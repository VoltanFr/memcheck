using System;
using System.Globalization;

namespace MemCheck.Basics;

public static class DateServices
{
    public static void CheckUTC(DateTime d)
    {
        if (d.Kind != DateTimeKind.Utc)
            throw new ArgumentException("Date not UTC - We want all dates in the app and in the DB to be UTC");
    }
    public static string AsIso(this DateTime d)
    {
        return d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }
    public static bool IsWithinPreviousDays(this DateTime d, uint dayCount)
    {
        return (DateTime.UtcNow - d).TotalDays <= dayCount;
    }
}
