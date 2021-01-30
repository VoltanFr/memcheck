using MemCheck.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MemCheck.WebUI
{
    public class MemCheckClaimsFactory : UserClaimsPrincipalFactory<MemCheckUser, MemCheckUserRole>
    {
        public MemCheckClaimsFactory(UserManager<MemCheckUser> userManager, RoleManager<MemCheckUserRole> roleManager, IOptions<IdentityOptions> optionsAccessor)
            : base(userManager, roleManager, optionsAccessor)
        {
        }
        public async override Task<ClaimsPrincipal> CreateAsync(MemCheckUser user)
        {
            var principal = await base.CreateAsync(user);
            ((ClaimsIdentity)principal.Identity!).AddClaims(new[] { new Claim(MemCheckClaims.UICulture, user.UILanguage ?? MemCheckRequestCultureProvider.French.Name) });
            return principal;
        }
    }
}