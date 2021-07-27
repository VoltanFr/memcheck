using MemCheck.Domain;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;

namespace MemCheck.WebUI
{
    public class MemCheckSignInManager : SignInManager<MemCheckUser>
    {
        public MemCheckSignInManager(UserManager<MemCheckUser> userManager, IHttpContextAccessor contextAccessor, IUserClaimsPrincipalFactory<MemCheckUser> claimsFactory, IOptions<IdentityOptions> optionsAccessor, ILogger<SignInManager<MemCheckUser>> logger, IAuthenticationSchemeProvider schemes, IUserConfirmation<MemCheckUser> confirmation) : base(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation)
        {
        }
        public override Task SignOutAsync()
        {
            MemCheckRequestCultureProvider.AddCultureCookie(Context.Features, Context.Response, MemCheckRequestCultureProvider.French.Name, true);
            return base.SignOutAsync();
        }
    }
}