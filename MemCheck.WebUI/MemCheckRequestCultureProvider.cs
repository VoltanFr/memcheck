using MemCheck.Application.Languages;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MemCheck.WebUI;

//We want to use the culture from the user's profile when available. This is done by using a claim (created by MemCheckClaimsFactory).
//If no user is logged in, we try to read the cookie.
//If not available, we use the culture passed by the browser.
public sealed class MemCheckRequestCultureProvider : RequestCultureProvider
{
    public MemCheckRequestCultureProvider()
    {
    }
    public override async Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
    {
        if (httpContext.User.Identity != null && httpContext.User.Identity.IsAuthenticated)
        {
            var cultureIdFromClaim = httpContext.User.FindFirstValue(MemCheckClaims.UICulture);
            if (cultureIdFromClaim != null && MemCheckSupportedCultures.CultureFromId(cultureIdFromClaim) != null)
            {
                var cultureFromClaim = MemCheckSupportedCultures.CultureFromId(cultureIdFromClaim);
                if (cultureFromClaim != null)
                {
                    return new ProviderCultureResult(cultureFromClaim.Name);
                }
            }
        }

        if (httpContext.Request.Cookies.TryGetValue("usrLang", out var cookieValue))
            if (cookieValue != null)
            {
                var cultureFromCookie = MemCheckSupportedCultures.CultureFromId(cookieValue);
                if (cultureFromCookie != null)
                    return new ProviderCultureResult(cultureFromCookie.Name);
            }

        //We will then use AcceptLanguageHeaderRequestCultureProvider (as configured in Startup)
        await Task.CompletedTask;
        return null;
    }
}
