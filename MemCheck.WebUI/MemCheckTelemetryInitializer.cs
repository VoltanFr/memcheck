using Microsoft.ApplicationInsights.AspNetCore.TelemetryInitializers;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Http;
using System.Linq;

namespace MemCheck.WebUI
{
    public class MemCheckTelemetryInitializer : TelemetryInitializerBase
    {
        public MemCheckTelemetryInitializer(IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
        }

        protected override void OnInitializeTelemetry(HttpContext platformContext, RequestTelemetry requestTelemetry, ITelemetry telemetry)
        {
            var user = platformContext.User;
            var userIdClaim = user.Claims.FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
            telemetry.Context.User.AccountId = userIdClaim?.Value;
            telemetry.Context.User.AuthenticatedUserId = user?.Identity?.Name;
        }
    }
}
