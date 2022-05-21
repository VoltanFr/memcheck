using MemCheck.Application.Languages;
using MemCheck.Application.QueryValidation;
using MemCheck.Application.Users;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace MemCheck.WebUI;

public sealed class Startup
{
    #region Fields
    private readonly bool prodEnvironment;
    private readonly IConfiguration configuration;
    #endregion
    #region Private methods
    private static ILogger<AppSettings> CreateLogger()
    {
        using ILoggerFactory? loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Trace);
            builder.AddConsole();
            builder.AddEventSourceLogger();
        });

        return loggerFactory.CreateLogger<AppSettings>();
    }
    private static async Task ControllerExceptionHandlerAsync(HttpContext context)
    {
        var e = context.Features.Get<IExceptionHandlerPathFeature>()!.Error;
        var ToastTitle = MemCheckSupportedCultures.French.Equals(context.Features.Get<IRequestCultureFeature>()?.RequestCulture?.Culture) ? "Échec" : "Failure";
        var ShowStatus = e is not RequestInputException;
        var ToastText = e is RequestInputException ? e.Message : ($"Exception class {e.GetType().Name}, message: '{e.Message}'" + (e.InnerException == null ? "" : $"\r\nInner exception class {e.InnerException.GetType().Name}, message: '{e.InnerException.Message}'"));
        await context.Response.WriteAsJsonAsync(new { ToastTitle, ToastText, ShowStatus });
    }
    #endregion
    public Startup(IConfiguration configuration, IWebHostEnvironment currentEnvironment)
    {
        this.configuration = configuration;
        prodEnvironment = currentEnvironment.IsProduction();
    }
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddLocalization(option => option.ResourcesPath = "Resources");
        services.AddMvc(option =>
        {
            option.EnableEndpointRouting = false;
        });

        var appSettings = new AppSettings(configuration, prodEnvironment, CreateLogger());

        if (!prodEnvironment)
            services.AddDatabaseDeveloperPageExceptionFilter();

        services.AddDbContext<MemCheckDbContext>(options => options.UseSqlServer(appSettings.ConnectionString));

        services.AddIdentity<MemCheckUser, MemCheckUserRole>(
            options =>
                {
                    options.SignIn.RequireConfirmedAccount = true;
                    options.User.RequireUniqueEmail = false;
                })
            .AddEntityFrameworkStores<MemCheckDbContext>()
            .AddDefaultTokenProviders()
            .AddUserManager<MemCheckUserManager>()
            .AddDefaultUI()
            .AddErrorDescriber<LocalizedIdentityErrorDescriber>()
            .AddSignInManager<MemCheckSignInManager>()
            .AddClaimsPrincipalFactory<MemCheckClaimsFactory>();

        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminPolicy", policy => policy.RequireRole(IRoleChecker.AdminRoleName));
        });

        services.AddSingleton<IEmailSender>(s => new SendGridEmailSender(appSettings.SendGrid));

        services.AddRazorPages().AddRazorPagesOptions(config =>
            {
                config.Conventions.AuthorizeFolder("/Decks");
                config.Conventions.AuthorizeFolder("/RepeatExpired");
                config.Conventions.AuthorizeFolder("/Authoring");
                config.Conventions.AuthorizeFolder("/Media");

                config.Conventions.AuthorizePage("/Tags/Authoring");

                config.Conventions.AuthorizeAreaFolder("Identity", "/Account/Manage");
                config.Conventions.AuthorizeAreaPage("Identity", "/Account/Logout");

                config.Conventions.AuthorizeFolder("/Admin", "AdminPolicy");
            })
            .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
            .AddDataAnnotationsLocalization();

        services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/Identity/Account/Login";
            options.LogoutPath = "/Identity/Account/Logout";
            options.AccessDeniedPath = "/Identity/Account/AccessDenied";
        });

        services.Configure<CookiePolicyOptions>(options =>
        {
            options.CheckConsentNeeded = context => true;
            options.MinimumSameSitePolicy = SameSiteMode.Strict;
        });

        services.AddApplicationInsightsTelemetry();
        services.AddSingleton<ITelemetryInitializer, MemCheckTelemetryInitializer>();
    }
    public void Configure(IApplicationBuilder app)
    {
        app.UseExceptionHandler(a => a.Run(async context => await ControllerExceptionHandlerAsync(context)));
        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseCookiePolicy();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();

        app.UseRequestLocalization(options =>
        {
            options.DefaultRequestCulture = new RequestCulture(MemCheckSupportedCultures.English);
            options.SupportedCultures = MemCheckSupportedCultures.All;    //Culture is used for numbers, dates, etc.
            options.SupportedUICultures = MemCheckSupportedCultures.All; //UI culture is used for looking up translations from resource files
            options.RequestCultureProviders = new IRequestCultureProvider[] { new MemCheckRequestCultureProvider(), new AcceptLanguageHeaderRequestCultureProvider() };
        });

        app.UseMvc();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapRazorPages();
        });
    }
}
