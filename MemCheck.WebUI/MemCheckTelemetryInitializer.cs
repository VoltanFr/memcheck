using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Http;
using System;

namespace MemCheck.WebUI
{
    public class MemCheckTelemetryInitializer : ITelemetryInitializer
    {
        private readonly IHttpContextAccessor httpContextAccessor;

        public MemCheckTelemetryInitializer(IHttpContextAccessor httpContextAccessor)
        {
            this.httpContextAccessor = httpContextAccessor;
        }

        public void Initialize(ITelemetry telemetry)
        {
            if (telemetry is not RequestTelemetry requestTelemetry)
                return;

            var context = httpContextAccessor.HttpContext;

            if (context == null)
            {
                requestTelemetry.Properties.Add("UserName", "null context");
                return;
            }

            var user = context.User;

            if (user == null)
            {
                requestTelemetry.Properties.Add("UserName", "null user");
                return;
            }

            var identity = user.Identity;

            if (identity == null)
            {
                requestTelemetry.Properties.Add("UserName", "null identity");
                return;
            }

            var userName = identity.Name;

            if (userName == null)
            {
                requestTelemetry.Properties.Add("UserName", "null user name");
                return;
            }

            requestTelemetry.Properties.Add("UserName", userName);
        }
    }
}
