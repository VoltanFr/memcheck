using MemCheck.Database;

namespace MemCheck.Application.Tests.Helpers
{
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
            return new CallContext(dbContext, new FakeMemCheckTelemetryClient(), new TestLocalizer());
        }
        public static CallContext AsCallContext(this MemCheckDbContext dbContext, TestLocalizer testLocalizer)
        {
            return new CallContext(dbContext, new FakeMemCheckTelemetryClient(), testLocalizer);
        }
    }
}
