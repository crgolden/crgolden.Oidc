namespace Clarity.Oidc
{
    using Core;
    using IdentityServer4.EntityFramework.Interfaces;
    using Microsoft.ApplicationInsights.AspNetCore;
    using Microsoft.ApplicationInsights.SnapshotCollector;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.ApplicationModels;
    using Microsoft.Azure.ServiceBus;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
    using Options;
    using Shared;

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
            services.AddApplicationInsightsTelemetry(_configuration)
                .AddDbContext<OidcDbContext>(_configuration.GetDbContextOptions(assemblyName: "Oidc.Data"))
                .Configure<SnapshotCollectorConfiguration>(_configuration.GetSection(nameof(SnapshotCollectorConfiguration)))
                .Configure<CookieTempDataProviderOptions>(options => options.Cookie.IsEssential = true)
                .Configure<EmailOptions>(_configuration.GetSection(nameof(EmailOptions)))
                .Configure<UserOptions>(_configuration.GetSection(nameof(UserOptions)))
                .Configure<CorsOptions>(_configuration.GetSection(nameof(CorsOptions)))
                .Configure<ServiceBusOptions>(_configuration.GetSection(nameof(ServiceBusOptions)))
                .ConfigureOptions<StaticFilesPostConfigureOptions>()
                .AddScoped<DbContext, OidcDbContext>()
                .AddScoped<IConfigurationDbContext, OidcDbContext>()
                .AddScoped<IPersistedGrantDbContext, OidcDbContext>()
                .ConfigureApplicationCookie(options =>
                {
                    options.LoginPath = "/account/login";
                    options.LogoutPath = "/account/logout";
                    options.AccessDeniedPath = "/account/access-denied";
                    options.ReturnUrlParameter = "returnUrl";
                    options.SlidingExpiration = true;
                })
                .AddScoped<ISeedService, SeedDataService>()
                .AddSingleton<IEmailService, SendGridEmailService>()
                .AddSingleton<ITelemetryProcessorFactory>(sp => new SnapshotCollectorTelemetryProcessorFactory(sp))
                .AddSingleton<IQueueClient, EmailQueueClient>()
                .AddCors()
                .AddKendo();
            services.AddHealthChecks();
            services.AddIdentity<User, Role>(setup => setup.SignIn.RequireConfirmedEmail = true)
                .AddEntityFrameworkStores<OidcDbContext>()
                .AddDefaultTokenProviders();
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
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IOptions<CorsOptions> corsOptions)
        {
            if (_environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage().UseDatabaseErrorPage();
            }
            else
            {
                app.UseHsts().UseExceptionHandler("/home/error");
            }

            app.UseHttpsRedirection()
                .UseStaticFiles()
                .UseCookiePolicy()
                .UseIdentityServer()
                .UseHealthChecks("/health")
                .UseCors(corsOptions.Value)
                .UseMvcWithDefaultRoute();
        }
    }
}
