using MemCheck.Database;

namespace MemCheck.Application.Tests.Helpers
{
    public sealed class FakeMemCheckTelemetryClient : IMemCheckTelemetryClient
    {
        public static CallContext InCallContext(MemCheckDbContext dbContext)
        {
            return new CallContext(dbContext, new FakeMemCheckTelemetryClient(), new TestLocalizer());
        }
        public void TrackEvent(string eventName, params (string key, string value)[] properties)
        {
        }
    }
}
