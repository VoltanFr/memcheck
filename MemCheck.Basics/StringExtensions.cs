namespace MemCheck.Basics
{
    public static class StringExtensions
    {
        public static string Truncate(this string str, int maxLength)
        {
            if (string.IsNullOrEmpty(str))
                return "";

            if (str.Length <= maxLength)
                return str;

            return str.Substring(0, maxLength) + "[...]";
        }
    }
}
