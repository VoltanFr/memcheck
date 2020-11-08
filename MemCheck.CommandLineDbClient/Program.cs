using MemCheck.Database;
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
    /*
    public class StructuredData
    {
        // Only public properties get serialized and show up in the logs.
        public string PublicStringProperty { get; set; }
        public int PublicIntProperty { get; set; }

        // The following properties/fields will not show up in the logs.
        private string PrivateStringProperty { get; set; }

        public string PublicStringField;
        private string PrivateStringField;

        public StructuredData()
        {
            PublicStringProperty = "Public property value";
            PublicIntProperty = 1;
            PrivateStringProperty = "Private property value";
            PublicStringField = "Public field value";
            PrivateStringField = "Private field value";
        }
        public override string ToString()
        {
            return PrivateStringField;
        }
    }
    public class ClassThatLogs
    {
        private readonly ILogger<ClassThatLogs> _logger;

        public ClassThatLogs(ILogger<ClassThatLogs> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void WriteLogs()
        {
            var structuredData = new StructuredData();
            var simpleData = "This is a string.";

            _logger.LogTrace("Here's a Verbose message.");
            _logger.LogDebug("Here's a Debug message. Only Public Properties (not fields) are shown on structured data. Structured data: {@sampleData}. Simple data: {simpleData}.", structuredData, simpleData);
            _logger.LogInformation(new Exception("Exceptions can be put on all log levels"), "Here's an Info message.");
            _logger.LogWarning("Here's a Warning message.");
            _logger.LogError(new Exception("This is an exception."), "Here's an Error message.");
            _logger.LogCritical("Here's a Fatal message.");
        }
    }
    */

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
