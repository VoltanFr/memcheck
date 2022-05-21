using System;
using System.Globalization;

namespace MemCheck.Application;

public abstract class ClassWithMetrics
{
    public static (string key, string value) IntMetric(string key, int value)
    {
        return (key, value.ToString(CultureInfo.InvariantCulture));
    }
    protected static (string key, string value) DoubleMetric(string key, double value)
    {
        return (key, value.ToString(CultureInfo.InvariantCulture));
    }
    protected static (string key, string value) DurationMetric(string key, TimeSpan value)
    {
        return (key, value.TotalMilliseconds.ToString(CultureInfo.InvariantCulture));
    }
    protected static (string key, string value) GuidMetric(string key, Guid value)
    {
        return (key, value.ToString());
    }
}
