using MemCheck.Domain;
using Microsoft.AspNetCore.Identity;
using System;
using System.Threading.Tasks;

namespace MemCheck.WebUI.Controllers
{
    internal static class UserServices
    //This class has to be replaceable by a user service registered in Startup, which knows the userManager
    {
        public static async Task<Guid> UserIdFromContextAsync(Microsoft.AspNetCore.Http.HttpContext httpContext, UserManager<MemCheckUser> userManager)
        {
            var contextUser = httpContext.User;
            var user = await userManager.GetUserAsync(contextUser);
            if (user == null)
                return Guid.Empty;
            return user.Id;
        }
    }
}
