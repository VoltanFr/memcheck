using System;

namespace MemCheck.Application
{
    public static class DateServices
    {
        public static void CheckUTC(DateTime d)
        {
            if (d.Kind != DateTimeKind.Utc)
                throw new ArgumentException("Date not UTC - We want all dates in the app and in the DB to be UTC");
        }
    }
}
