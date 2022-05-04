using MemCheck.Application.Users;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;

[assembly: FunctionsStartup(typeof(MemCheck.AzureFunctions.Startup))]

namespace MemCheck.AzureFunctions;
public class Startup : FunctionsStartup
{
    public override void Configure(IFunctionsHostBuilder builder)
    {
        builder.Services.AddDbContext<MemCheckDbContext>(options => SqlServerDbContextOptionsExtensions.UseSqlServer(options, Environment.GetEnvironmentVariable("MemCheckDbConnectionString")));

        builder.Services.AddIdentityCore<MemCheckUser>(opt =>
        {
            opt.Password.RequireDigit = false;
            opt.Password.RequireLowercase = false;
            opt.Password.RequireNonAlphanumeric = false;
            opt.Password.RequireUppercase = false;
        })
            .AddSignInManager()
            .AddEntityFrameworkStores<MemCheckDbContext>()
            .AddUserManager<MemCheckUserManager>()
            .AddDefaultTokenProviders();


        //Microsoft.AspNetCore.Identity.IdentityBuilder identityBuilder = builder.Services
        //            .AddIdentity<MemCheckUser, MemCheckUserRole>(
        //            options =>
        //                {
        //                    options.SignIn.RequireConfirmedAccount = true;
        //                    options.User.RequireUniqueEmail = false;
        //                });


        //identityBuilder
        //    .AddEntityFrameworkStores<MemCheckDbContext>()
        //    .AddUserManager<MemCheckUserManager>();
    }
}
