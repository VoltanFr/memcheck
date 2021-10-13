﻿using MemCheck.Application;
using MemCheck.Database;

namespace MemCheck.CommandLineDbClient
{
    public sealed class FakeMemCheckTelemetryClient : IMemCheckTelemetryClient
    {
        public static CallContext InCallContext(MemCheckDbContext dbContext)
        {
            return new CallContext(dbContext, new FakeMemCheckTelemetryClient(), new FakeStringLocalizer());
        }
        public void TrackEvent(string eventName, params (string key, string value)[] properties)
        {
        }
    }
}