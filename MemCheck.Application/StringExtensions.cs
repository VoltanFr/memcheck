namespace MemCheck.Application
{
    public static class StringExtensions
    {
        public static string Truncate(this string str, int maxLength, bool withEllipsisIfTruncated = false)
        {
            if (string.IsNullOrEmpty(str))
                return "";

            if (str.Length <= maxLength)
                return str;

            return str.Substring(0, maxLength) + "'[...]";
        }
    }
}