namespace MemCheck.Application.QueryValidation
{
    public sealed class FakeStringLocalizer : ILocalized
    {
        public string Get(string resourceName)
        {
            return "no translation";
        }
    }
}
