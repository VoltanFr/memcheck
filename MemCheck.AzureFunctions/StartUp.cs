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

        builder.Services.AddIdentityCore<MemCheckUser>(opt => { opt.SignIn.RequireConfirmedAccount = true; opt.User.RequireUniqueEmail = false; })
            .AddSignInManager()
            .AddRoles<MemCheckUserRole>()
            .AddUserManager<MemCheckUserManager>()
            .AddEntityFrameworkStores<MemCheckDbContext>()
            .AddDefaultTokenProviders();
    }
}
