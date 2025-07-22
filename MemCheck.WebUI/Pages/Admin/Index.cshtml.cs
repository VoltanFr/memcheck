using MemCheck.Application.QueryValidation;
using MemCheck.Basics;
using MemCheck.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace MemCheck.WebUI.Pages.Admin;

[Authorize(Roles = IRoleChecker.AdminRoleName)]
public sealed class IndexModel : PageModel
{
    private readonly IWebHostEnvironment currentEnvironment;

    [BindProperty] public string WebRootPath { get; set; } = null!;
    [BindProperty] public string ApplicationName { get; set; } = null!;
    [BindProperty] public string EntryAssembly { get; set; } = null!;
    [BindProperty] public string EnvironmentName { get; set; } = null!;
    [BindProperty] public string EmailSenderAddress { get; set; } = null!;
    [BindProperty] public bool Is64BitProcess { get; set; } = false;
    [BindProperty] public string OSVersion { get; set; } = null!;
    [BindProperty] public int ProcessorCount { get; set; } = 0;
    [BindProperty] public string ProcessWorkingSet { get; set; } = null!;
    [BindProperty] public string EnvironmentVersion { get; set; } = null!;
    [BindProperty] public IEnumerable<string> MemCheckAssemblies { get; set; } = null!;
    public IndexModel(IWebHostEnvironment currentEnvironment, IMemCheckMailSender emailSender)
    {
        this.currentEnvironment = currentEnvironment;
        EmailSenderAddress = emailSender.SenderAddress.Address;
    }
    public void OnGet()
    {
        WebRootPath = currentEnvironment.WebRootPath;
        ApplicationName = currentEnvironment.ApplicationName;
        EnvironmentName = currentEnvironment.EnvironmentName;
        Is64BitProcess = Environment.Is64BitProcess;
        OSVersion = Environment.OSVersion.VersionString;
        ProcessorCount = Environment.ProcessorCount;
        EnvironmentVersion = Environment.Version.ToString();
        ProcessWorkingSet = Environment.WorkingSet.ToString("N0", CultureInfo.CurrentCulture);
        EntryAssembly = AssemblyServices.GetDisplayInfoForAssembly(Assembly.GetEntryAssembly());
        var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => a.FullName!.StartsWith("MemCheck", StringComparison.OrdinalIgnoreCase));
        MemCheckAssemblies = assemblies.Select(a => AssemblyServices.GetDisplayInfoForAssembly(a)).OrderBy(a => a);
    }
}
