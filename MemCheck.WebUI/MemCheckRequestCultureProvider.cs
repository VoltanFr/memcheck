using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Localization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MemCheck.WebUI
{
    //We want to use the culture from the user's profile when available. This is done by using a claim (created by MemCheckClaimsFactory). In other words, we don't use a culture cookie.
    //If not available, use the culture passed by the browser header (not implemented yet)
    //In case of error, default to french
    //See also UILanguagesController.SetCultureAsync
    //MS Documentation: https://docs.microsoft.com/en-us/aspnet/core/fundamentals/localization?view=aspnetcore-5.0
    public class MemCheckRequestCultureProvider : RequestCultureProvider
    {
        #region Fields
        private static readonly CultureInfo english = new("en-US");
        private static readonly CultureInfo french = new("fr-FR");
        #endregion
        public async override Task<ProviderCultureResult> DetermineProviderCultureResult(HttpContext httpContext)
        {
            string? cultureFromCookie = null;
            if (httpContext.Request.Cookies.TryGetValue(CookieRequestCultureProvider.DefaultCookieName, out var cutureCookie))
                cultureFromCookie = CookieRequestCultureProvider.ParseCookieValue(cutureCookie).UICultures.FirstOrDefault().Value;

            //Priority 1: use culture from user profile
            if (httpContext.User.Identity != null && httpContext.User.Identity.IsAuthenticated)
            {
                var result = httpContext.User.FindFirstValue(MemCheckClaims.UICulture);
                if (result != null && IsValidCulture(result))
                {
                    if (cultureFromCookie != result)
                        AddCultureCookie(httpContext.Features, httpContext.Response, result, false);
                    return new ProviderCultureResult(result);
                }
            }

            //Priority 2: use culture from cookie (don't forget that event if user is not logged in we want to keep his language choice)
            if (cultureFromCookie != null && IsValidCulture(cultureFromCookie))
                return new ProviderCultureResult(cultureFromCookie);

            //Priority 3: use browser's requested culture
            //To be implemented. See https://stackoverflow.com/questions/49381843/get-browser-language-in-aspnetcore2-0

            //Default behavior: return French
            return await Task.FromResult(new ProviderCultureResult(french.Name));
        }
        public static CultureInfo English => english;
        public static CultureInfo French => french;
        public static IEnumerable<CultureInfo> SupportedCultures => new[] { English, French };
        public static bool IsValidCulture(string cultureName)
        {
            return SupportedCultures.Any(c => c.Name == cultureName);
        }
        public static void AddCultureCookie(IFeatureCollection contextFeatures, HttpResponse response, string cultureName, bool expired)
        {
            if (!contextFeatures.Get<ITrackingConsentFeature>().CanTrack)
                return;
            var cookieOptions = new CookieOptions { Secure = true, HttpOnly = true, SameSite = SameSiteMode.Strict, Expires = DateTimeOffset.UtcNow.AddYears(expired ? -1 : 1) };
            response.Cookies.Append(CookieRequestCultureProvider.DefaultCookieName, CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(cultureName)), cookieOptions);
        }
    }
}