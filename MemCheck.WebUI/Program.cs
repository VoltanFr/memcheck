using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MemCheck.WebUI
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }
        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            IHostBuilder result = Host.CreateDefaultBuilder(args);
            result = result.ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>());
            result = result.ConfigureLogging((context, builder) => builder.AddFilter("", context.HostingEnvironment.IsProduction() ? LogLevel.Warning : LogLevel.Debug));
            return result;
        }
    }
}
