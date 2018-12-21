namespace Cef.OIDC
{
    using Core.Extensions;
    using Core.Factories;
    using Core.Filters;
    using Core.Interfaces;
    using Core.Options;
    using Core.Services;
    using Data;
    using Extensions;
    using IdentityServer4.EntityFramework.Interfaces;
    using Microsoft.ApplicationInsights.AspNetCore;
    using Microsoft.ApplicationInsights.SnapshotCollector;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Identity.UI.Services;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Models;
    using Services;

    public class Startup
    {
        private readonly IConfiguration _configuration;
        private readonly IHostingEnvironment _environment;

        public Startup(IConfiguration configuration, IHostingEnvironment environment)
        {
            _configuration = configuration;
            _environment = environment;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddApplicationInsightsTelemetry(_configuration);
            services.AddDbContext<OidcDbContext>(_configuration.GetDbContextOptions());
            services.Configure<SnapshotCollectorConfiguration>(_configuration.GetSection(nameof(SnapshotCollectorConfiguration)));
            services.Configure<CookieTempDataProviderOptions>(options => options.Cookie.IsEssential = true);
            services.Configure<EmailOptions>(_configuration.GetSection(nameof(EmailOptions)));
            services.Configure<Options.UserOptions>(_configuration.GetSection(nameof(Options.UserOptions)));
            services.AddScoped<DbContext, OidcDbContext>();
            services.AddScoped<IConfigurationDbContext, OidcDbContext>();
            services.AddScoped<IPersistedGrantDbContext, OidcDbContext>();
            services.AddIdentity<User, Role>(setup => setup.SignIn.RequireConfirmedEmail = true)
                .AddEntityFrameworkStores<OidcDbContext>()
                .AddDefaultTokenProviders();
            services.AddScoped<ISeedService, SeedDataService>();
            services.AddSingleton<IEmailSender, EmailSender>();
            services.AddSingleton<ITelemetryProcessorFactory>(sp => new SnapshotCollectorTelemetryProcessorFactory(sp));
            services.AddCors();
            services.AddMvc(setup => setup.Filters.Add(typeof(ModelStateFilter)))
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
            services.AddIdentityServer(_configuration, _environment);
            services.AddAuthentication(_configuration);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseHsts();
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();
            app.UseIdentityServer();
            app.UseCors(_configuration);
            app.UseMvcWithDefaultRoute();

            loggerFactory.AddApplicationInsights(app.ApplicationServices, LogLevel.Warning);
        }
    }
}
