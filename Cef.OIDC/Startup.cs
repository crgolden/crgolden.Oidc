namespace Cef.OIDC
{
    using Core.DbContexts;
    using Core.Extensions;
    using Core.Filters;
    using Core.Interfaces;
    using Core.Models;
    using Core.Options;
    using Core.Services;
    using Extensions;
    using IdentityServer4.EntityFramework.Interfaces;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Identity.UI.Services;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Services;

    public class Startup
    {
        private readonly IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });
            services.AddApplicationInsightsTelemetry(_configuration);
            services.AddDatabase(_configuration);
            services.AddIdentity<User, Role>(setup => setup.SignIn.RequireConfirmedEmail = true)
                .AddEntityFrameworkStores<CefDbContext>()
                .AddDefaultTokenProviders();
            services.AddScoped<IConfigurationDbContext, CefDbContext>();
            services.AddScoped<IPersistedGrantDbContext, CefDbContext>();
            services.AddScoped<ISeedService, SeedDataService>();
            services.AddSingleton<IEmailSender, EmailSender>();
            services.AddCorsOptions(_configuration);
            services.AddEmailOptions(_configuration);
            services.AddUserOptions(_configuration);
            services.AddAuthentication(_configuration);
            services.AddPolicies();
            services.AddMvc(setup => setup.Filters.Add(typeof(ModelStateFilter)))
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_1)
                .AddRazorPagesOptions(options =>
                {
                    options.AllowAreas = true;
                    options.Conventions.AuthorizeAreaFolder("Identity", "/Account/Manage");
                    options.Conventions.AuthorizeAreaPage("Identity", "/Account/Logout");
                });
            services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Identity/Account/Login";
                options.LogoutPath = "/Identity/Account/Logout";
                options.AccessDeniedPath = "/Identity/Account/AccessDenied";
            });
            services.AddIdentityServer(_configuration.GetDbContextOptions());
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env,
            ILoggerFactory loggerFactory, IOptions<CorsOptions> corsOptions)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();
            app.UseIdentityServer();
            app.UseCors(corsOptions.Value);
            app.UseMvcWithDefaultRoute();

            loggerFactory.AddAzureWebAppDiagnostics();
            loggerFactory.AddApplicationInsights(app.ApplicationServices, LogLevel.Warning);
        }
    }
}
