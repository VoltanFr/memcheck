using MemCheck.Application.QueryValidation;

namespace MemCheck.AzureFunctions;

public sealed class FakeStringLocalizer : ILocalized
{
    public string Get(string resourceName)
    {
        return resourceName;
    }
}
