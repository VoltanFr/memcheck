using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace MemCheck.Application.Helpers;

public static class DateAssert
{
    private static void IsBefore(DateTime expected, DateTime actual)
    {
        Assert.IsTrue(actual <= expected);
    }
    private static void IsAfter(DateTime expected, DateTime actual)
    {
        Assert.IsTrue(actual >= expected);
    }
    public static void IsInRange(DateTime expected, TimeSpan allowedTimeSpanAfter, DateTime actual)
    {
        IsAfter(expected, actual);
        IsBefore(expected.Add(allowedTimeSpanAfter), actual);
    }
}
