using MemCheck.Application.QueryValidation;
using MemCheck.Basics;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.AspNetCore.Authentication.Cookies;
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
using System;
using System.IO;
using System.Linq;

namespace MemCheck.WebUI
{
    public sealed class Startup
    {
        #region Fields
        private readonly bool prodEnvironment;
        private readonly IConfiguration configuration;
        #endregion
        #region Private methods
        private string GetConnectionString()
        {
            if (prodEnvironment)
                return configuration[$"ConnectionStrings:AzureDbConnection"];

            if (configuration["ConnectionStrings:DebuggingDb"] == "Local")
                return configuration[$"ConnectionStrings:LocalDbConnection"];

            if (configuration["ConnectionStrings:DebuggingDb"] == "Azure")
                return File.ReadAllText(@"C:\BackedUp\DocsBV\Synchronized\SkyDrive\Programmation\MemCheck private info\AzureConnectionString.txt").Trim();

            throw new IOException($"Invalid DebuggingDb '{configuration["ConnectionStrings:DebuggingDb"]}'");
        }
        private void ConfigureDataBase(IServiceCollection services)
        {
            services.AddDatabaseDeveloperPageExceptionFilter();
            var connectionString = GetConnectionString();
            services.AddDbContext<MemCheckDbContext>(options => options.UseSqlServer(connectionString));
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
            }).AddRazorPagesOptions(options =>
            {
                options.Conventions.AuthorizeAreaFolder("Identity", "/Account/Manage");
                options.Conventions.AuthorizeAreaPage("Identity", "/Account/Logout");
            });

            ConfigureDataBase(services);

            services.AddIdentity<MemCheckUser, MemCheckUserRole>(options => { options.SignIn.RequireConfirmedAccount = true; })
                .AddEntityFrameworkStores<MemCheckDbContext>()
                .AddDefaultTokenProviders()
                .AddDefaultUI()
                .AddErrorDescriber<LocalizedIdentityErrorDescriber>()
                .AddClaimsPrincipalFactory<MemCheckClaimsFactory>();

            services.AddAuthorization(options =>
            {
                options.AddPolicy("AdminPolicy", policy => policy.RequireRole("Admin"));
            });

            services.AddTransient<IEmailSender, SendGridEmailSender>();
            services.Configure<AuthMessageSenderOptions>(options => configuration.Bind(options));

            services.AddRazorPages().AddRazorPagesOptions(config =>
                {
                    config.Conventions.AuthorizeFolder("/Decks");
                    config.Conventions.AuthorizeFolder("/Learn");
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
                options.LoginPath = "/Identity/Pages/Account/Login";
                options.LogoutPath = "/Identity/Pages/Account/Logout";
                options.AccessDeniedPath = "/Identity/Pages/Account/AccessDenied";
            });

            services.Configure<CookiePolicyOptions>(options =>
            {
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;  //To be understood
            });
        }
        public void Configure(IApplicationBuilder app)
        {
            app.UseExceptionHandler(a => a.Run(async context =>
            {
                var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
                var e = exceptionHandlerPathFeature.Error;
                var cultureName = context.Features.Get<IRequestCultureFeature>().RequestCulture.Culture.Name;
                var ToastTitle = cultureName.Equals("FR", StringComparison.OrdinalIgnoreCase) ? "Échec" : "Failure";
                var ShowStatus = !(e is RequestInputException);
                var ToastText = e is RequestInputException ? e.Message : ($"Exception class {e.GetType().Name}, message: '{e.Message}'" + (e.InnerException == null ? "" : $"\r\nInner exception class {e.InnerException.GetType().Name}, message: '{e.InnerException.Message}'"));
                await context.Response.WriteAsJsonAsync(new { ToastTitle, ToastText, ShowStatus });
            }));

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseRequestLocalization(options =>
            {
                options.DefaultRequestCulture = new RequestCulture(MemCheckRequestCultureProvider.English);
                options.SupportedCultures = MemCheckRequestCultureProvider.SupportedCultures.ToArray();    //Culture is used for numbers, dates, etc.
                options.SupportedUICultures = MemCheckRequestCultureProvider.SupportedCultures.ToArray(); //UI culture is used for looking up translations from resource files
                options.RequestCultureProviders = new MemCheckRequestCultureProvider().AsArray();
            });

            app.UseMvc();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
            });
        }
    }
}