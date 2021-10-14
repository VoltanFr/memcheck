using MemCheck.Database;

namespace MemCheck.Application.Tests.Helpers
{
    internal sealed class FakeMemCheckTelemetryClient : IMemCheckTelemetryClient
    {
        public static CallContext InCallContext(MemCheckDbContext dbContext)
        {
            return new CallContext(dbContext, new FakeMemCheckTelemetryClient(), new TestLocalizer());
        }
        public static CallContext InCallContext(MemCheckDbContext dbContext, TestLocalizer testLocalizer)
        {
            return new CallContext(dbContext, new FakeMemCheckTelemetryClient(), testLocalizer);
        }
        public void TrackEvent(string eventName, params (string key, string value)[] properties)
        {
        }
    }
}
