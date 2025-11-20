using System;
using System.Linq;
using System.Threading.Tasks;
using MemCheck.Application.Users;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MemCheck.AzureFunctions;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine($"MemCheck.AzureFunctions, {DateTime.Now:s}");

        var builder = FunctionsApplication.CreateBuilder(args);

        builder.Services
            .AddApplicationInsightsTelemetryWorkerService()
            .ConfigureFunctionsApplicationInsights();

        builder.Logging.Services.Configure<LoggerFilterOptions>(options =>
        {
            // The Application Insights SDK adds a default logging filter that instructs ILogger to capture only Warning and more severe logs. Application Insights requires an explicit override.
            // Log levels can also be configured using appsettings.json. For more information, see https://learn.microsoft.com/azure/azure-monitor/app/worker-service#ilogger-logs
            var defaultRule = options.Rules.FirstOrDefault(rule => rule.ProviderName == "Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider");
            if (defaultRule is not null)
            {
                options.Rules.Remove(defaultRule);
            }
        });

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

        var host = builder.Build();

        await host.RunAsync();
    }
}
