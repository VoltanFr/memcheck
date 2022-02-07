using MemCheck.Application.Users;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;

[assembly: FunctionsStartup(typeof(MemCheck.AzureFunctions.StartUp))]

namespace MemCheck.AzureFunctions;
public class StartUp : FunctionsStartup
{
    public override void Configure(IFunctionsHostBuilder builder)
    {
        //builder.Services.AddDbContext<MemCheckDbContext>(options => SqlServerDbContextOptionsExtensions.UseSqlServer(options, Environment.GetEnvironmentVariable("MemCheckDbConnectionString")));

        //builder.Services.AddIdentity<MemCheckUser, MemCheckUserRole>(
        //    options =>
        //        {
        //            options.SignIn.RequireConfirmedAccount = true;
        //            options.User.RequireUniqueEmail = false;
        //        })
        //    .AddEntityFrameworkStores<MemCheckDbContext>()
        //    .AddUserManager<MemCheckUserManager>();
    }
}
