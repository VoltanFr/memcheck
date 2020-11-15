using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MemCheck.CommandLineDbClient
{
    internal static class Program
    {
        #region Private methods
        private static IConfiguration GetConfig()
        {
            return new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
        }
        private static void SetupStaticLogger(IConfiguration config)
        {
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(config)
                .CreateLogger();
        }
        private static IHostBuilder CreateHostBuilder(IConfiguration config)
        {
            IHostBuilder hostBuilder = new HostBuilder();
            hostBuilder = hostBuilder.ConfigureServices((hostContext, services) =>
                   {
                       services
                       // Setup Dependency Injection container.
                       //.AddTransient(typeof(ClassThatLogs))
                       .AddHostedService<Engine>()
                       .AddDbContext<MemCheckDbContext>(options => options.UseSqlServer(config[$"ConnectionStrings:DbConnection"]));

                       services.AddIdentity<MemCheckUser, MemCheckUserRole>(options =>
                       {
                           options.SignIn.RequireConfirmedAccount = true;
                       })
    .AddEntityFrameworkStores<MemCheckDbContext>()
    .AddDefaultTokenProviders()
    .AddDefaultUI();
                   }
                );
            hostBuilder = hostBuilder.ConfigureLogging((hostContext, logging) => logging.AddSerilog());
            return hostBuilder;
        }
        #endregion
        public static void Main()
        {
            var config = GetConfig();
            SetupStaticLogger(config);
            try
            {
                CreateHostBuilder(config).RunConsoleAsync();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "An unhandled exception occurred.");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
