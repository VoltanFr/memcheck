using System.Text.RegularExpressions;

namespace MemCheck.CommandLineDbClient.RegionsFrancaises;

internal static class StringExtensions
{
    public static string RemoveBlanks(this string s)
    {
        return Regex.Replace(s, @"\s+", "");
    }
}
