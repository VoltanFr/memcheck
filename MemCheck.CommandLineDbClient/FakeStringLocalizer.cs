using MemCheck.Application.QueryValidation;

namespace MemCheck.CommandLineDbClient
{
    public sealed class FakeStringLocalizer : ILocalized
    {
        public string Get(string resourceName)
        {
            return "no translation";
        }
    }

}
