using MemCheck.Application.QueryValidation;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

namespace MemCheck.Application.Helpers;

internal sealed class TestLocalizer : ILocalized
{
    #region Fields
    private readonly ImmutableDictionary<string, string> values;
    #endregion
    public TestLocalizer(params KeyValuePair<string, string>[] items)
    {
        values = ImmutableDictionary.CreateRange(items ?? System.Array.Empty<KeyValuePair<string, string>>());
    }
    [DebuggerStepThrough]
    public string GetLocalized(string resourceName)
    {
        return values.TryGetValue(resourceName, out var value) ? value : "";
    }
}
