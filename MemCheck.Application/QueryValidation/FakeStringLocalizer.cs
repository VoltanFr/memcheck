namespace MemCheck.Application.QueryValidation
{
    public sealed class FakeStringLocalizer : ILocalized
    {
        public string GetLocalized(string resourceName)
        {
            return "no translation";
        }
    }
}
