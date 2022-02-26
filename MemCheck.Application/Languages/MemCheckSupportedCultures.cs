using System.Collections.Immutable;
using System.Globalization;

namespace MemCheck.Application.Languages
{
    public static class MemCheckSupportedCultures
    {
        #region Fields
        private static readonly CultureInfo english = new("en-US");
        private static readonly CultureInfo french = new("fr-FR");
        #endregion
        public static CultureInfo English => english;
        public static CultureInfo French => french;
        public static ImmutableArray<CultureInfo> All => new[] { English, French }.ToImmutableArray();
        public static string? IdFromCulture(CultureInfo c)
        {
            if (English.Equals(c))
                return "En";
            if (French.Equals(c))
                return "Fr";
            return null;
        }
        public static CultureInfo? CultureFromId(string culture)
        {
            switch (culture)
            {
                case "En":
                    return English;
                case "Fr":
                    return French;
                default:
                    return null;
            }
        }
    }
}
