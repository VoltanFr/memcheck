using System;
using System.Diagnostics;
using System.Globalization;

namespace MemCheck.Basics;

public static class ProcessServices
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<Pending>")]
    public static string GetPeakProcessMemoryUsage()
    {
        try
        {
            return Process.GetCurrentProcess().PeakWorkingSet64.ToString(CultureInfo.InvariantCulture);
        }
        catch (Exception e)
        {
            return e.Message;
        }
    }
}
