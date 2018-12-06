namespace Cef.OIDC.Extensions
{
    using System;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;
    using Core.Extensions;
    using Core.Models;
    using Core.Options;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;

    public static class ServiceCollectionExtensions
    {
        public static void AddIdentityServer(this IServiceCollection services, IConfiguration configuration, IHostingEnvironment environment)
        {
            var dbContextOptions = configuration.GetDbContextOptions();
            var isDevelopment = environment.IsDevelopment();
            var builder = services
                .AddIdentityServer(config => config.Authentication.CookieLifetime = TimeSpan.FromHours(2))
                // this adds the config data from DB (clients, resources, CORS)
                .AddConfigurationStore(options => options.ConfigureDbContext = dbContextOptions)
                // this adds the operational data from DB (codes, tokens, consents)
                .AddOperationalStore(options =>
                {
                    options.ConfigureDbContext = dbContextOptions;
                    if (!isDevelopment) { return; }

                    // this enables automatic token cleanup. this is optional.
                    options.EnableTokenCleanup = true;
                    options.TokenCleanupInterval = 30; // interval in seconds, short for testing
                })
                .AddAspNetIdentity<User>();

            if (isDevelopment)
            {
                builder.AddDeveloperSigningCredential();
            }
            else
            {
                // this is something you will want in production to reduce load on and requests to the DB
                builder.AddConfigurationStoreCache();

                X509Certificate2 PfxStringToCert(string pfx)
                {
                    var bytes = Convert.FromBase64String(pfx);
                    var coll = new X509Certificate2Collection();
                    coll.Import(
                        rawData: bytes,
                        password: null,
                        keyStorageFlags: X509KeyStorageFlags.Exportable |
                                         X509KeyStorageFlags.MachineKeySet |
                                         X509KeyStorageFlags.EphemeralKeySet |
                                         X509KeyStorageFlags.PersistKeySet |
                                         X509KeyStorageFlags.DefaultKeySet);
                    return coll[0];
                }

                var signingCredential = configuration.GetValue<string>("SigningCredential");
                if (!string.IsNullOrEmpty(signingCredential))
                {
                    builder.AddSigningCredential(PfxStringToCert(signingCredential));
                }

                var validationKey = configuration.GetValue<string>("ValidationKey");
                if (!string.IsNullOrEmpty(validationKey))
                {
                    builder.AddValidationKey(PfxStringToCert(validationKey));
                }
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