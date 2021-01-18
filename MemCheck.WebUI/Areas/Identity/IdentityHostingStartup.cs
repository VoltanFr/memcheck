using Microsoft.AspNetCore.Hosting;

[assembly: HostingStartup(typeof(MemCheck.WebUI.Areas.Identity.IdentityHostingStartup))]
namespace MemCheck.WebUI.Areas.Identity
{
    public class IdentityHostingStartup : IHostingStartup
    {
        public void Configure(IWebHostBuilder builder)
        {
            builder.ConfigureServices((context, services) =>
            {
            });
        }
    }
}