using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MemCheck.WebUI.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public sealed class IndexModel : PageModel
    {
        private readonly IWebHostEnvironment currentEnvironment;

        [BindProperty] public string WebRootPath { get; set; } = null!;
        [BindProperty] public string ApplicationName { get; set; } = null!;
        [BindProperty] public string EntryAssembly { get; set; } = null!;
        [BindProperty] public string EnvironmentName { get; set; } = null!;
        [BindProperty] public IEnumerable<string> MemCheckAssemblies { get; set; } = null!;
        public IndexModel(IWebHostEnvironment currentEnvironment)
        {
            this.currentEnvironment = currentEnvironment;
        }
        private static string GetDisplayInfoForAssembly(Assembly? a)
        {
            if (a == null)
                return "Unknown";

            var informationalVersionAttribute = a.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            var version = informationalVersionAttribute == null ? "Unknown" : informationalVersionAttribute.InformationalVersion;
            return a.GetName().Name + ' ' + version;

        }
        public void OnGet()
        {
            WebRootPath = currentEnvironment.WebRootPath;
            ApplicationName = currentEnvironment.ApplicationName;
            EnvironmentName = currentEnvironment.EnvironmentName;
            EntryAssembly = GetDisplayInfoForAssembly(Assembly.GetEntryAssembly());
            var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => a.FullName!.StartsWith("MemCheck"));
            MemCheckAssemblies = assemblies.Select(a => GetDisplayInfoForAssembly(a)).OrderBy(a => a);
        }
    }
}
