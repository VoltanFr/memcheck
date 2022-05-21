using MemCheck.Application;
using MemCheck.Application.QueryValidation;
using MemCheck.Database;

namespace MemCheck.CommandLineDbClient;

internal sealed class FakeMemCheckTelemetryClient : IMemCheckTelemetryClient
{
    public void TrackEvent(string eventName, params (string key, string value)[] properties)
    {
    }
}
internal static class DbContextExtensions
{
    public static CallContext AsCallContext(this MemCheckDbContext dbContext)
    {
        return new CallContext(dbContext, new FakeMemCheckTelemetryClient(), new FakeStringLocalizer(), new TestRoleChecker());
    }
    public static CallContext AsCallContext(this MemCheckDbContext dbContext, FakeStringLocalizer fakeLocalizer)
    {
        return new CallContext(dbContext, new FakeMemCheckTelemetryClient(), fakeLocalizer, new TestRoleChecker());
    }
    public static CallContext AsCallContext(this MemCheckDbContext dbContext, IRoleChecker roleChecker)
    {
        return new CallContext(dbContext, new FakeMemCheckTelemetryClient(), new FakeStringLocalizer(), roleChecker);
    }
}
