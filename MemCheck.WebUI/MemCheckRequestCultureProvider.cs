using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MemCheck.WebUI
{
    //We want to use the culture from the user's profile when available. This is done by using a claim (created by MemCheckClaimsFactory). In other words, we don't use a culture cookie (this is done by the JavaScript).
    //If not available, use the culture passed by the browser header (not implemented yet)
    //In case of error, default to french
    //See also UILanguagesController.SetCultureAsync
    //MS Documentation: https://docs.microsoft.com/en-us/aspnet/core/fundamentals/localization?view=aspnetcore-5.0
    public class MemCheckRequestCultureProvider : RequestCultureProvider
    {
        #region Fields
        private static readonly CultureInfo english = new("en-US");
        private static readonly CultureInfo french = new("fr-FR");
        private readonly ILogger logger;
        #endregion
        public MemCheckRequestCultureProvider(ILogger logger)
        {
            this.logger = logger;
        }
        public async override Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
        {
            logger.LogDebug($"Http request path: {httpContext.Request.Path}");

            if (httpContext.User.Identity != null && httpContext.User.Identity.IsAuthenticated)
            {
                var result = httpContext.User.FindFirstValue(MemCheckClaims.UICulture);
                if (result != null && IsValidCulture(result))
                    return new ProviderCultureResult(result);
            }

            //To do: use the browser's requested culture instead of defaulting to French. See https://stackoverflow.com/questions/49381843/get-browser-language-in-aspnetcore2-0

            return await Task.FromResult(new ProviderCultureResult(french.Name));
        }
        public static CultureInfo English => english;
        public static CultureInfo French => french;
        public static IEnumerable<CultureInfo> SupportedCultures => new[] { English, French };
        public static bool IsValidCulture(string cultureName)
        {
            return SupportedCultures.Any(c => c.Name == cultureName);
        }
    }
}