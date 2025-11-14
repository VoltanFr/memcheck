using MemCheck.Database;

namespace MemCheck.Application.Helpers;

internal sealed class FakeMemCheckTelemetryClient : IMemCheckTelemetryClient
{
    public void TrackEvent(string eventName, params (string key, string value)[] properties)
    {
    }
}
public static class DbContextExtensions
{
    public static CallContext AsCallContext(this MemCheckDbContext dbContext)
    {
        return new CallContext(dbContext, new FakeMemCheckTelemetryClient(), new TestLocalizer(), new TestRoleChecker());
    }
    public static CallContext AsCallContext(this MemCheckDbContext dbContext, TestLocalizer testLocalizer)
    {
        return new CallContext(dbContext, new FakeMemCheckTelemetryClient(), testLocalizer, new TestRoleChecker());
    }
    public static CallContext AsCallContext(this MemCheckDbContext dbContext, TestRoleChecker roleChecker)
    {
        return new CallContext(dbContext, new FakeMemCheckTelemetryClient(), new TestLocalizer(), roleChecker);
    }
    public static CallContext AsCallContext(this MemCheckDbContext dbContext, TestLocalizer testLocalizer, TestRoleChecker roleChecker)
    {
        return new CallContext(dbContext, new FakeMemCheckTelemetryClient(), testLocalizer, roleChecker);
    }
}
