namespace Clarity.Oidc
{
    using Core;
    using Data;
    using Extensions;
    using IdentityServer4.EntityFramework.Interfaces;
    using Interfaces;
    using Microsoft.ApplicationInsights.AspNetCore;
    using Microsoft.ApplicationInsights.SnapshotCollector;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.ApplicationModels;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
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
            services.Configure<CorsOptions>(_configuration.GetSection(nameof(CorsOptions)));
            services.AddScoped<DbContext, OidcDbContext>();
            services.AddScoped<IConfigurationDbContext, OidcDbContext>();
            services.AddScoped<IPersistedGrantDbContext, OidcDbContext>();
            services.AddIdentity<User, Role>(setup => setup.SignIn.RequireConfirmedEmail = true)
                .AddEntityFrameworkStores<OidcDbContext>()
                .AddDefaultTokenProviders();
            services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/account/login";
                options.LogoutPath = "/account/logout";
                options.AccessDeniedPath = "/account/access-denied";
                options.ReturnUrlParameter = "returnUrl";
                options.SlidingExpiration = true;
            });
            services.AddScoped<ISeedService, SeedDataService>();
            services.AddSingleton<IEmailService, SendGridEmailService>();
            services.AddSingleton<ITelemetryProcessorFactory>(sp => new SnapshotCollectorTelemetryProcessorFactory(sp));
            services.AddHealthChecks();
            services.AddCors();
            services.AddMvc(setup =>
                {
                    setup.Conventions.Add(new RouteTokenTransformerConvention(new SlugifyParameterTransformer()));
                    setup.Filters.Add<ModelStateActionFilter>();
                    setup.Filters.Add<ModelStatePageFilter>();
                })
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_2)
                .AddRazorPagesOptions(setup => setup.Conventions.Add(new PageRouteTransformerConvention(new SlugifyParameterTransformer())));
            services.AddIdentityServer(_configuration, _environment);
            services.AddAuthentication(_configuration);
            services.AddKendo();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IOptions<CorsOptions> corsOptions)
        {
            if (_environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseHsts();
                app.UseExceptionHandler("/home/error");
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();
            app.UseIdentityServer();
            app.UseHealthChecks("/health");
            app.UseCors(corsOptions);
            app.UseMvcWithDefaultRoute();
        }
    }
}
