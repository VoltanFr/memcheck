using MemCheck.Application.QueryValidation;
using MemCheck.Basics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MemCheck.WebUI.Pages.Admin
{
    [Authorize(Roles = IRoleChecker.AdminRoleName)]
    public sealed class IndexModel : PageModel
    {
        private readonly IWebHostEnvironment currentEnvironment;

        [BindProperty] public string WebRootPath { get; set; } = null!;
        [BindProperty] public string ApplicationName { get; set; } = null!;
        [BindProperty] public string EntryAssembly { get; set; } = null!;
        [BindProperty] public string EnvironmentName { get; set; } = null!;
        [BindProperty] public string SendGridEmailSender { get; set; } = null!;
        [BindProperty] public IEnumerable<string> MemCheckAssemblies { get; set; } = null!;
        public IndexModel(IWebHostEnvironment currentEnvironment, IEmailSender emailSender)
        {
            this.currentEnvironment = currentEnvironment;
            SendGridEmailSender = WebUI.SendGridEmailSender.SenderFromInterface(emailSender);
        }
        public void OnGet()
        {
            WebRootPath = currentEnvironment.WebRootPath;
            ApplicationName = currentEnvironment.ApplicationName;
            EnvironmentName = currentEnvironment.EnvironmentName;
            EntryAssembly = AssemblyServices.GetDisplayInfoForAssembly(Assembly.GetEntryAssembly());
            var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => a.FullName!.StartsWith("MemCheck"));
            MemCheckAssemblies = assemblies.Select(a => AssemblyServices.GetDisplayInfoForAssembly(a)).OrderBy(a => a);
        }
    }
}
