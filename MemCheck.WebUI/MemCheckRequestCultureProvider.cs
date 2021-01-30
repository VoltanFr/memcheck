using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MemCheck.WebUI
{
    //We want to use the culture from the user's profile when available. This is done by using a claim (created by MemCheckClaimsFactory). In other words, we don't use a culture cookie.
    //If not available, use the culture passed by the browser header (not implemented yet)
    //In case of error, default to french
    public class MemCheckRequestCultureProvider : RequestCultureProvider
    {
        #region Fields
        private static readonly CultureInfo english = new CultureInfo("en-US");
        private static readonly CultureInfo french = new CultureInfo("fr-FR");
        #endregion
        public async override Task<ProviderCultureResult> DetermineProviderCultureResult(HttpContext httpContext)
        {
            if (httpContext.User.Identity == null || !httpContext.User.Identity.IsAuthenticated)
                return new ProviderCultureResult(english.Name); //To do: use the browser's requested culture instead

            var culture = httpContext.User.FindFirstValue(MemCheckClaims.UICulture);

            if (culture == null)
                culture = english.Name; //To do: use the browser's requested culture instead

            return await Task.FromResult(new ProviderCultureResult(culture));
        }
        public static CultureInfo English => english;
        public static CultureInfo French => french;
        public static IEnumerable<CultureInfo> SupportedCultures => new[] { English, French };
    }
}