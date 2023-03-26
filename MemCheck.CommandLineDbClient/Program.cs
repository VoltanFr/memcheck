using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace MemCheck.CommandLineDbClient;

internal static class Program
{
    #region Private methods
    private static string GetConnectionString(IConfiguration config)
    {
        if (config["ConnectionStrings:DebuggingDb"] == "Azure")
            return File.ReadAllText(@"C:\BackedUp\DocsBV\Synchronized\SkyDrive\Programmation\MemCheck-private-info\AzureConnectionString.txt").Trim();

        var db = config["ConnectionStrings:DebuggingDb"];
        if (!string.IsNullOrEmpty(config[$"ConnectionStrings:{db}"]))
            db = config[$"ConnectionStrings:{db}"];
        return db ?? throw new ConfigurationErrorsException("Unable to read connection string");
    }
    private static IConfiguration GetConfig()
    {
        return new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
    }
    private static IHostBuilder CreateHostBuilder(IConfiguration config)
    {
        IHostBuilder hostBuilder = new HostBuilder();
        var connectionString = GetConnectionString(config);

        hostBuilder = hostBuilder.ConfigureServices((hostContext, services) => services
            .AddHostedService<Engine>()
            .AddDbContext<MemCheckDbContext>(options => options.UseSqlServer(connectionString))
            .AddIdentity<MemCheckUser, MemCheckUserRole>(options => options.SignIn.RequireConfirmedAccount = true)
            .AddEntityFrameworkStores<MemCheckDbContext>()
            .AddDefaultTokenProviders()
            .AddDefaultUI());

        hostBuilder = hostBuilder.ConfigureLogging(logging =>
        {
            logging.AddConfiguration(config.GetSection("Logging"));
            logging.AddConsole();
        });

        return hostBuilder;
    }
    #endregion
    public static async Task Main()
    {
        var config = GetConfig();
        await CreateHostBuilder(config).RunConsoleAsync();
        Debugger.Break();
    }
}
