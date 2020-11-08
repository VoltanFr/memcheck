using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MemCheck.Database;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using MemCheck.Domain;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Localization;
using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;
using Microsoft.Extensions.Localization;

namespace MemCheck.WebUI
{
    public sealed class Startup
    {
        #region Fields
        private readonly bool prodEnvironment;
        private readonly IConfiguration configuration;
        #endregion
        #region Private methods
        private void ConfigureDataBase(IServiceCollection services)
        {
            var connectionStringKey = prodEnvironment ? "AzureDbConnection" : "LocalDbConnection";
            services.AddDbContext<MemCheckDbContext>(options => options.UseSqlServer(configuration[$"ConnectionStrings:{connectionStringKey}"]));
        }
        private void ConfigureErrorHandling(IApplicationBuilder app)
        {
            if (prodEnvironment)
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }
            else
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
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

            services.AddIdentity<MemCheckUser, MemCheckUserRole>(options =>
            {
                options.SignIn.RequireConfirmedAccount = true;
            })
                .AddEntityFrameworkStores<MemCheckDbContext>()
                .AddDefaultTokenProviders()
                .AddDefaultUI()
                .AddErrorDescriber<LocalizedIdentityErrorDescriber>();

            services.AddTransient<IEmailSender, SendGridEmailSender>();
            services.Configure<AuthMessageSenderOptions>(options => configuration.Bind(options));

            services.AddRazorPages()
                .AddRazorPagesOptions(config =>
                {
                    config.Conventions.AuthorizeFolder("/Decks");
                    config.Conventions.AuthorizeFolder("/LearnUnknown");
                    config.Conventions.AuthorizeFolder("/RepeatExpired");
                    config.Conventions.AuthorizeFolder("/Authoring");
                    config.Conventions.AuthorizeAreaFolder("Identity", "/Account/Manage");
                    config.Conventions.AuthorizeAreaPage("Identity", "/Account/Logout");
                })
                .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
                .AddDataAnnotationsLocalization();

            services.Configure<RequestLocalizationOptions>(options =>
            {
                var supportedCultures = new[]
                {
                    new CultureInfo("en"    ),
                    new CultureInfo("fr")
                };

                options.DefaultRequestCulture = new RequestCulture("en");
                options.SupportedCultures = supportedCultures;
                options.SupportedUICultures = supportedCultures;

                //options.AddInitialRequestCultureProvider(new CustomRequestCultureProvider(async context =>
                //{
                //    // My custom request culture logic
                //    return new ProviderCultureResult("en");
                //}));
            });

            services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = $"/Identity/Account/Login";
                options.LogoutPath = $"/Identity/Account/Logout";
                options.AccessDeniedPath = $"/Identity/Account/AccessDenied";
            });

            services.Configure<CookiePolicyOptions>(options =>
            {
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;  //To be understood
            });
        }
        public void Configure(IApplicationBuilder app)
        {
            ConfigureErrorHandling(app);

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseRequestLocalization(options =>
            {
                options.DefaultRequestCulture = new RequestCulture("en");
                CultureInfo[] cultureInfo = new[] { new CultureInfo("en"), new CultureInfo("fr") };
                options.SupportedCultures = cultureInfo;
                options.SupportedUICultures = cultureInfo;
            });
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseMvc();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
            });
        }
    }
}
