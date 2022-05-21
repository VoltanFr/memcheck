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
        builder.Services.AddDbContext<MemCheckDbContext>(options =>
        {
            var connectionString = Environment.GetEnvironmentVariable("MemCheckDbConnectionString");
            if (connectionString == null)
                throw new InvalidOperationException("MemCheckDbConnectionString not found");
            SqlServerDbContextOptionsExtensions.UseSqlServer(options, connectionString);
        });

        builder.Services.AddIdentityCore<MemCheckUser>(opt => { opt.SignIn.RequireConfirmedAccount = true; opt.User.RequireUniqueEmail = false; })
            .AddRoles<MemCheckUserRole>()
            .AddUserManager<MemCheckUserManager>()
            .AddEntityFrameworkStores<MemCheckDbContext>();
    }
}
