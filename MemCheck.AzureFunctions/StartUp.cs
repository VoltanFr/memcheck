using MemCheck.Application.Users;
using MemCheck.Database;
using MemCheck.Domain;
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

        builder.Services.AddIdentity<MemCheckUser, MemCheckUserRole>(
            options =>
                {
                    options.SignIn.RequireConfirmedAccount = true;
                    options.User.RequireUniqueEmail = false;
                })
            .AddEntityFrameworkStores<MemCheckDbContext>()
            .AddUserManager<MemCheckUserManager>();
    }
}
