using System.Collections.Immutable;
using System.Globalization;

namespace MemCheck.Application.Languages;

public static class MemCheckSupportedCultures
{
    #region Fields
    #endregion
    public static CultureInfo English { get; } = new("en-US");
    public static CultureInfo French { get; } = new("fr-FR");
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
        return culture switch
        {
            "En" => English,
            "Fr" => French,
            _ => null,
        };
    }
}
