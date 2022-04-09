using MemCheck.Application.QueryValidation;

namespace MemCheck.AzureFunctions;

public sealed class FakeStringLocalizer : ILocalized
{
    public string GetLocalized(string resourceName)
    {
        return resourceName;
    }
}
