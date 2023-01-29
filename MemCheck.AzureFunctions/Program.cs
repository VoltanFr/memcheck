﻿using System;
using MemCheck.Application.Users;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MemCheck.AzureFunctions;

internal static class Program
{
    public static void Main(/*string[] args*/)
    {
        var host = new HostBuilder()
            .ConfigureFunctionsWorkerDefaults(builder =>
            {
                builder.AddApplicationInsights().AddApplicationInsightsLogger();
            })
            .ConfigureServices(services =>
                {
                    services.AddDbContext<MemCheckDbContext>(options =>
                    {
                        var connectionString = Environment.GetEnvironmentVariable("MemCheckDbConnectionString");
                        if (connectionString == null)
                            throw new InvalidOperationException("MemCheckDbConnectionString not found");
                        SqlServerDbContextOptionsExtensions.UseSqlServer(options, connectionString);
                    });

                    services.AddIdentityCore<MemCheckUser>(opt => { opt.SignIn.RequireConfirmedAccount = true; opt.User.RequireUniqueEmail = false; })
                        .AddRoles<MemCheckUserRole>()
                        .AddUserManager<MemCheckUserManager>()
                        .AddEntityFrameworkStores<MemCheckDbContext>();
                })
            .ConfigureLogging(loggingBuilder =>
                {
                    loggingBuilder.AddSimpleConsole();
                }
            )
            .Build();

        host.Run();
    }
}
