using System.Text.RegularExpressions;

namespace MemCheck.CommandLineDbClient.Pauker
{
    internal static class StringExtensions
    {
        public static string RemoveBlanks(this string s)
        {
            return Regex.Replace(s, @"\s+", "");
        }
    }
}
