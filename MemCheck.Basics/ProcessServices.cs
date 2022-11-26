using System;
using System.Diagnostics;
using System.Globalization;

namespace MemCheck.Basics;

public static class ProcessServices
{
    public static string GetPeakProcessMemoryUsage()
    {
        try
        {
            return Process.GetCurrentProcess().PeakWorkingSet64.ToString("N0", CultureInfo.GetCultureInfo("en"));
        }
        catch (Exception e)
        {
            return e.Message;
        }
    }
}
