using MemCheck.Domain;
using Microsoft.AspNetCore.Identity;
using System;
using System.Threading.Tasks;

namespace MemCheck.WebUI.Controllers;

internal static class UserServices
//This class has to be replaceable by a user service registered in Startup, which knows the userManager
{
    public static async Task<Guid> UserIdFromContextAsync(Microsoft.AspNetCore.Http.HttpContext httpContext, UserManager<MemCheckUser> userManager)
    {
        return (await UserFromContextAsync(httpContext, userManager)).Id;
    }
    public static async Task<MemCheckUser> UserFromContextAsync(Microsoft.AspNetCore.Http.HttpContext httpContext, UserManager<MemCheckUser> userManager)
    {
        var contextUser = httpContext.User;
        var user = await userManager.GetUserAsync(contextUser);
        return user ?? throw new InvalidOperationException("No user");
    }
}
