using MemCheck.Database;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections;

[assembly: FunctionsStartup(typeof(MemCheck.AzureFunctions.Startup))]

namespace MemCheck.AzureFunctions;

public class Startup : FunctionsStartup
{
    public override void Configure(IFunctionsHostBuilder builder)
    {
        Console.WriteLine("****** Env vars");
        var envVars = Environment.GetEnvironmentVariables();
        foreach (DictionaryEntry envVar in envVars)
            Console.WriteLine($"'{envVar.Key}'='{envVar.Value}'");

        builder.Services.AddDbContext<MemCheckDbContext>(options => SqlServerDbContextOptionsExtensions.UseSqlServer(options, Environment.GetEnvironmentVariable("MemCheckDbConnectionString")));

        //builder.Services.AddIdentityCore<MemCheckUser>(opt => { opt.SignIn.RequireConfirmedAccount = true; opt.User.RequireUniqueEmail = false; })
        //    .AddRoles<MemCheckUserRole>()
        //    .AddUserManager<MemCheckUserManager>()
        //    .AddEntityFrameworkStores<MemCheckDbContext>();
    }
}
