using MemCheck.Application;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.WebUI.Controllers
{
    public static class UserServices
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
