using MemCheck.Application;
using MemCheck.Application.QueryValidation;
using MemCheck.Application.Users;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MemCheck.WebUI
{
    public class MemCheckClaimsFactory : UserClaimsPrincipalFactory<MemCheckUser, MemCheckUserRole>
    {
        #region Fields
        private readonly CallContext callContext;
        #endregion
        public MemCheckClaimsFactory(UserManager<MemCheckUser> userManager, RoleManager<MemCheckUserRole> roleManager, IOptions<IdentityOptions> optionsAccessor, MemCheckDbContext dbContext, TelemetryClient telemetryClient)
            : base(userManager, roleManager, optionsAccessor)
        {
            callContext = new CallContext(dbContext, new MemCheckTelemetryClient(telemetryClient), new FakeStringLocalizer(), new ProdRoleChecker(userManager));
        }
        public override async Task<ClaimsPrincipal> CreateAsync(MemCheckUser user)
        {
            var principal = await base.CreateAsync(user);
            if (user.UILanguage != null)
                ((ClaimsIdentity)principal.Identity!).AddClaims(new[] { new Claim(MemCheckClaims.UICulture, user.UILanguage) });

            await new UpdateUserLastSeenDate(callContext).RunAsync(new UpdateUserLastSeenDate.Request(user.Id));

            return principal;
        }
    }
}