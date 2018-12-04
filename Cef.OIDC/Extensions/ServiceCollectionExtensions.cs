namespace Cef.OIDC.Extensions
{
    using System;
    using System.Threading.Tasks;
    using Core.Models;
    using Core.Options;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;

    public static class ServiceCollectionExtensions
    {
        public static void AddIdentityServer(this IServiceCollection services, Action<DbContextOptionsBuilder> dbContextOptions, IHostingEnvironment environment)
        {
            var builder = services
                .AddIdentityServer(config =>
                {
                    config.Authentication.CookieLifetime = TimeSpan.FromHours(2);
                })
                // this adds the config data from DB (clients, resources, CORS)
                .AddConfigurationStore(options => options.ConfigureDbContext = dbContextOptions)
                // this adds the operational data from DB (codes, tokens, consents)
                .AddOperationalStore(options =>
                {
                    options.ConfigureDbContext = dbContextOptions;
                    // this enables automatic token cleanup. this is optional.
                    options.EnableTokenCleanup = true;
                    options.TokenCleanupInterval = 30; // interval in seconds, short for testing
                })
                .AddAspNetIdentity<User>()
                // this is something you will want in production to reduce load on and requests to the DB
                .AddConfigurationStoreCache();

            if (environment.IsDevelopment())
            {
                builder.AddDeveloperSigningCredential();
            }
            else
            {

            }
        }

        public static void AddAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var authenticationOptionsSection = configuration.GetSection(nameof(AuthenticationOptions));
            if (!authenticationOptionsSection.Exists()) { return; }

            var authenticationOptions = authenticationOptionsSection.Get<AuthenticationOptions>();
            if (authenticationOptions?.Facebook == null) { return; }

            services.AddAuthentication()
                .AddFacebook(options =>
                {
                    options.AppId = authenticationOptions.Facebook.AppId;
                    options.AppSecret = authenticationOptions.Facebook.AppSecret;
                    options.SignInScheme = IdentityConstants.ExternalScheme;
                    options.Events.OnRemoteFailure = context =>
                    {
                        context.Response.Redirect($"{context.Properties.Items["Referer"]}");
                        context.HandleResponse();
                        return Task.CompletedTask;
                    };
                });
        }
    }
}