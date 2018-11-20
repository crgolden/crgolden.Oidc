namespace Cef.OIDC.Extensions
{
    using System;
    using Cef.Core.Models;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;

    public static class ServiceCollectionExtensions
    {
        public static void AddIdentityServer(this IServiceCollection services, Action<DbContextOptionsBuilder> dbContextOptions)
        {
            // configure identity server with in-memory stores, keys, clients and scopes
            services
                .AddIdentityServer(config =>
                {
                    config.IssuerUri = "null";
                    config.Authentication.CookieLifetime = TimeSpan.FromHours(2);
                })
                .AddDeveloperSigningCredential()
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
        }
    }
}