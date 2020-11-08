using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MemCheck.WebUI.Pages.Admin
{
    public sealed class IndexModel : PageModel
    {
        private readonly IWebHostEnvironment currentEnvironment;

        [BindProperty] public string WebRootPath { get; set; } = null!;
        [BindProperty] public string ApplicationName { get; set; } = null!;
        [BindProperty] public string EntryAssemblyName { get; set; } = null!;
        [BindProperty] public string EntryAssemblyVersion { get; set; } = null!;
        [BindProperty] public string EnvironmentName { get; set; } = null!;
        [BindProperty] public IEnumerable<string> MemCheckAssemblies { get; set; } = null!;
        [BindProperty] public IEnumerable<string> OtherAssemblies { get; set; } = null!;
        public IndexModel(IWebHostEnvironment currentEnvironment)
        {
            this.currentEnvironment = currentEnvironment;
        }
        public void OnGet()
        {
            WebRootPath = currentEnvironment.WebRootPath;
            ApplicationName = currentEnvironment.ApplicationName;
            Assembly? entryAssembly = Assembly.GetEntryAssembly();
            EntryAssemblyName = entryAssembly == null ? "Unknown" : (entryAssembly.FullName == null ? "Unknown (no full name)" : entryAssembly.FullName.ToString());
            AssemblyFileVersionAttribute? fileVersionAttribute = entryAssembly?.GetCustomAttribute<AssemblyFileVersionAttribute>();
            EntryAssemblyVersion = fileVersionAttribute == null ? "Unknown" : fileVersionAttribute.Version;
            EnvironmentName = currentEnvironment.EnvironmentName;
            var assemblies = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.FullName == null ? a.GetName().ToString() : a.FullName).OrderBy(a => a);
            MemCheckAssemblies = assemblies.Where(assemblyName => assemblyName.StartsWith("MemCheck"));
            OtherAssemblies = assemblies.Where(assemblyName => !assemblyName.StartsWith("MemCheck"));
        }
    }
}
